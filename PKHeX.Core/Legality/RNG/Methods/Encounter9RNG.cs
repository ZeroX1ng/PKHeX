using System;
using System.Runtime.CompilerServices;

namespace PKHeX.Core;

public static class Encounter9RNG
{
    public static bool TryApply32(this EncounterTera9 enc, PK9 pk, in ulong init, in GenerateParam9 param, EncounterCriteria criteria)
    {
        const int maxCtr = 100_000;
        var rand = new Xoroshiro128Plus(init);
        for (int ctr = 0; ctr < maxCtr; ctr++)
        {
            uint seed = (uint)rand.NextInt(uint.MaxValue);
            if (!Tera9RNG.IsMatchStarChoice(seed, enc.Stars, enc.RandRate, enc.RandRateMinScarlet, enc.RandRateMinViolet))
                continue;
            if (!GenerateData(pk, param, criteria, seed))
                continue;

            var type = Tera9RNG.GetTeraType(seed, enc.TeraType, enc.Species, enc.Form);
            pk.TeraTypeOriginal = (MoveType)type;
            if (criteria.TeraType != -1 && type != criteria.TeraType)
                pk.SetTeraType(type); // sets the override type
            return true; // done.
        }
        return false;
    }

    public static bool TryApply64<TEnc>(this TEnc enc, PK9 pk, in ulong init, in GenerateParam9 param, EncounterCriteria criteria, bool ignoreIVs)
        where TEnc : ISpeciesForm, IGemType
    {
        var rand = new Xoroshiro128Plus(init);
        const int maxCtr = 100_000;
        for (int ctr = 0; ctr < maxCtr; ctr++)
        {
            ulong seed = rand.Next(); // fake cryptosecure
            if (!GenerateData(pk, param, criteria, seed, ignoreIVs))
                continue;

            var type = Tera9RNG.GetTeraType(seed, enc.TeraType, enc.Species, enc.Form);
            pk.TeraTypeOriginal = (MoveType)type;
            if (criteria.TeraType != -1 && type != criteria.TeraType)
                pk.SetTeraType(type); // sets the override type
            return true; // done.
        }
        return false;
    }

    /// <summary>
    /// Fills out an entity with details from the provided encounter template.
    /// </summary>
    /// <returns>False if the seed cannot generate data matching the criteria.</returns>
    public static bool GenerateData(PK9 pk, in GenerateParam9 enc, EncounterCriteria criteria, in ulong seed, bool ignoreIVs = false)
    {
        var rand = new Xoroshiro128Plus(seed);
        pk.EncryptionConstant = (uint)rand.NextInt(uint.MaxValue);
        var fakeTID = (uint)rand.NextInt();
        uint pid = (uint)rand.NextInt();
        bool isShiny;
        uint xor;
        if (enc.Shiny == Shiny.Random) // let's decide if it's shiny or not!
        {
            xor = GetShinyXor(pid, fakeTID);
            isShiny = xor < 16;
            if (isShiny && xor != 0)
                xor = 1;
        }
        else
        {
            // no need to calculate a fake trainer
            xor = 0;
            isShiny = enc.Shiny == Shiny.Always;
        }

        ForceShinyState(pk, isShiny, ref pid, xor);
        pk.PID = pid;

        const int UNSET = -1;
        Span<int> ivs = stackalloc[] { UNSET, UNSET, UNSET, UNSET, UNSET, UNSET };
        const int MAX = 31;
        for (int i = 0; i < enc.FlawlessIVs; i++)
        {
            int index;
            do { index = (int)rand.NextInt(6); }
            while (ivs[index] != UNSET);
            ivs[index] = MAX;
        }

        for (int i = 0; i < 6; i++)
        {
            if (ivs[i] == UNSET)
                ivs[i] = (int)rand.NextInt(32);
        }

        if (!ignoreIVs && !criteria.IsIVsCompatible(ivs, 9))
            return false;

        pk.IV_HP = ivs[0];
        pk.IV_ATK = ivs[1];
        pk.IV_DEF = ivs[2];
        pk.IV_SPA = ivs[3];
        pk.IV_SPD = ivs[4];
        pk.IV_SPE = ivs[5];

        int abil = enc.Ability switch
        {
            AbilityPermission.Any12H => (int)rand.NextInt(3) << 1,
            AbilityPermission.Any12 => (int)rand.NextInt(2) << 1,
            _ => (int)enc.Ability,
        };
        pk.RefreshAbility(abil >> 1);

        var gender_ratio = enc.GenderRatio;
        int gender = gender_ratio switch
        {
            PersonalInfo.RatioMagicGenderless => 2,
            PersonalInfo.RatioMagicFemale => 1,
            PersonalInfo.RatioMagicMale => 0,
            _ => GetGender(gender_ratio, rand.NextInt(100)),
        };
        if (criteria.Gender != -1 && gender != criteria.Gender)
            return false;
        pk.Gender = gender;

        byte nature = pk.Species == (int)Species.Toxtricity
                ? ToxtricityUtil.GetRandomNature(ref rand, pk.Form)
                : (byte)rand.NextInt(25);
        if (criteria.Nature != Nature.Random && nature != (int)criteria.Nature)
            return false;
        pk.Nature = pk.StatNature = nature;

        pk.HeightScalar = enc.Height != 0 ? enc.Height : (byte)(rand.NextInt(0x81) + rand.NextInt(0x80));
        pk.WeightScalar = enc.Weight != 0 ? enc.Weight : (byte)(rand.NextInt(0x81) + rand.NextInt(0x80));
        pk.Scale        = enc.Scale  != 0 ? enc.Scale  : (byte)(rand.NextInt(0x81) + rand.NextInt(0x80));
        return true;
    }

    public static bool IsMatch(PKM pk, in GenerateParam9 enc, in ulong seed)
    {
        // same as above method
        var rand = new Xoroshiro128Plus(seed);
        if (pk.EncryptionConstant != (uint)rand.NextInt(uint.MaxValue))
            return false;

        var fakeTID = (uint)rand.NextInt();
        uint pid = (uint)rand.NextInt();
        bool isShiny;
        uint xor;
        if (enc.Shiny == Shiny.Random) // let's decide if it's shiny or not!
        {
            int i = 1;
            while (true)
            {
                xor = GetShinyXor(pid, fakeTID);
                isShiny = xor < 16;
                if (isShiny)
                {
                    if (xor != 0)
                        xor = 1;
                    break;
                }
                if (i >= enc.RollCount)
                    break;
                pid = (uint)rand.NextInt();
                i++;
            }
        }
        else
        {
            // no need to calculate a fake trainer
            isShiny = enc.Shiny == Shiny.Always;
            xor = 0;
        }

        ForceShinyState(pk, isShiny, ref pid, xor);

        if (pk.PID != pid)
            return false;

        const int UNSET = -1;
        Span<int> ivs = stackalloc[] { UNSET, UNSET, UNSET, UNSET, UNSET, UNSET };
        const int MAX = 31;
        for (int i = 0; i < enc.FlawlessIVs; i++)
        {
            int index;
            do { index = (int)rand.NextInt(6); }
            while (ivs[index] != UNSET);
            ivs[index] = MAX;
        }

        for (int i = 0; i < 6; i++)
        {
            if (ivs[i] == UNSET)
                ivs[i] = (int)rand.NextInt(32);
        }

        if (pk.IV_HP != ivs[0])
            return false;
        if (pk.IV_ATK != ivs[1])
            return false;
        if (pk.IV_DEF != ivs[2])
            return false;
        if (pk.IV_SPA != ivs[3])
            return false;
        if (pk.IV_SPD != ivs[4])
            return false;
        if (pk.IV_SPE != ivs[5])
            return false;

        int abil = enc.Ability switch
        {
            AbilityPermission.Any12H => (int)rand.NextInt(3) << 1,
            AbilityPermission.Any12 => (int)rand.NextInt(2) << 1,
            _ => (int)enc.Ability,
        };

        var gender_ratio = enc.GenderRatio;
        int gender = gender_ratio switch
        {
            PersonalInfo.RatioMagicGenderless => 2,
            PersonalInfo.RatioMagicFemale => 1,
            PersonalInfo.RatioMagicMale => 0,
            _ => GetGender(gender_ratio, rand.NextInt(100)),
        };
        if (pk.Gender != gender)
            return false;

        int nature = pk.Species == (int)Species.Toxtricity
                ? ToxtricityUtil.GetRandomNature(ref rand, pk.Form)
                : (byte)rand.NextInt(25);
        if (pk.Nature != nature)
            return false;

        if (enc.Height == 0)
        {
            var value = (int)rand.NextInt(0x81) + (int)rand.NextInt(0x80);
            if (pk is IScaledSize s && s.HeightScalar != value)
                return false;
        }
        if (enc.Weight == 0)
        {
            var value = (int)rand.NextInt(0x81) + (int)rand.NextInt(0x80);
            if (pk is IScaledSize s && s.WeightScalar != value)
                return false;
        }
        if (enc.Scale == 0)
        {
            var value = (int)rand.NextInt(0x81) + (int)rand.NextInt(0x80);
            if (pk is IScaledSize3 s && s.Scale != value)
                return false;
        }
        return true;
    }

    public static int GetGender(in int ratio, in ulong rand100) => ratio switch
    {
        0x1F => rand100 < 12 ? 1 : 0, // 12.5%
        0x3F => rand100 < 25 ? 1 : 0, // 25%
        0x7F => rand100 < 50 ? 1 : 0, // 50%
        0xBF => rand100 < 75 ? 1 : 0, // 75%
        0xE1 => rand100 < 89 ? 1 : 0, // 87.5%

        _ => throw new ArgumentOutOfRangeException(nameof(ratio)),
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ForceShinyState(PKM pk, in bool isShiny, ref uint pid, uint xor)
    {
        if (isShiny)
        {
            if (!GetIsShiny(pk.TID, pk.SID, pid))
                pid = GetShinyPID(pk.TID, pk.SID, pid, (int)xor);
        }
        else
        {
            if (GetIsShiny(pk.TID, pk.SID, pid))
                pid ^= 0x1000_0000;
        }
    }

    private static uint GetShinyPID(in int tid, in int sid, in uint pid, in int type)
    {
        return (uint)(((tid ^ sid ^ (pid & 0xFFFF) ^ type) << 16) | (pid & 0xFFFF));
    }

    private static bool GetIsShiny(in int tid, in int sid, in uint pid)
    {
        return GetShinyXor(tid, sid, pid) < 16;
    }

    private static uint GetShinyXor(in int tid, in int sid, in uint pid)
    {
        return GetShinyXor(pid, (uint)((sid << 16) | tid));
    }

    private static uint GetShinyXor(in uint pid, in uint oid)
    {
        var xor = pid ^ oid;
        return (xor ^ (xor >> 16)) & 0xFFFF;
    }
}
