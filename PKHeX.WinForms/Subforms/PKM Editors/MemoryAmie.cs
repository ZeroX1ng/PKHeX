using System;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHeX.WinForms;

public partial class MemoryAmie : Form
{
    private readonly TextMarkup TextArgs;
    private readonly MemoryStrings MemStrings;

    public MemoryAmie(PKM pk)
    {
        InitializeComponent();
        WinFormsUtil.TranslateInterface(this, Main.CurrentLanguage);
        Entity = pk;
        MemStrings = new MemoryStrings(GameInfo.Strings);
        PrevCountries = new[] { CB_Country0, CB_Country1, CB_Country2, CB_Country3, CB_Country4 };
        PrevRegions = new[] { CB_Region0, CB_Region1, CB_Region2, CB_Region3, CB_Region4 };
        string[] arguments = L_Arguments.Text.Split(new[] {" ; "}, StringSplitOptions.None);

        TextArgs = new TextMarkup(arguments);
        foreach (ComboBox comboBox in PrevCountries)
        {
            comboBox.InitializeBinding();
            Main.SetCountrySubRegion(comboBox, "countries");
        }
        foreach (var region in PrevRegions)
            region.InitializeBinding();
        GetLangStrings();
        LoadFields();

        if (pk is not IGeoTrack)
            tabControl1.TabPages.Remove(Tab_Residence);
    }

    private bool init;
    private readonly ComboBox[] PrevCountries;
    private readonly ComboBox[] PrevRegions;
    private readonly PKM Entity;

    // Load/Save Actions
    private void LoadFields()
    {
        // Load the region/country values.
        if (Entity is IGeoTrack g)
        {
            CB_Country0.SelectedValue = (int)g.Geo1_Country;
            CB_Country1.SelectedValue = (int)g.Geo2_Country;
            CB_Country2.SelectedValue = (int)g.Geo3_Country;
            CB_Country3.SelectedValue = (int)g.Geo4_Country;
            CB_Country4.SelectedValue = (int)g.Geo5_Country;
            CB_Region0.SelectedValue  = (int)g.Geo1_Region;
            CB_Region1.SelectedValue  = (int)g.Geo2_Region;
            CB_Region2.SelectedValue  = (int)g.Geo3_Region;
            CB_Region3.SelectedValue  = (int)g.Geo4_Region;
            CB_Region4.SelectedValue  = (int)g.Geo5_Region;
        }

        // Load the Fullness, and Enjoyment
        M_Fullness.Text = Entity.Fullness.ToString();
        M_Enjoyment.Text = Entity.Enjoyment.ToString();

        M_OT_Friendship.Text = Entity.OT_Friendship.ToString();
        M_CT_Friendship.Text = Entity.HT_Friendship.ToString();

        if (Entity is IAffection a)
        {
            M_OT_Affection.Text = a.OT_Affection.ToString();
            M_CT_Affection.Text = a.HT_Affection.ToString();
        }

        if (Entity is ISociability s)
            MT_Sociability.Text = Math.Min(byte.MaxValue, s.Sociability).ToString();

        if (Entity is ITrainerMemories m)
        {
            // Load the OT Memories
            CB_OTQual.SelectedIndex = Math.Max(0, m.OT_Intensity - 1);
            CB_OTMemory.SelectedValue = (int)m.OT_Memory;
            CB_OTVar.SelectedValue = (int)m.OT_TextVar;
            CB_OTFeel.SelectedIndex = m.OT_Feeling;

            // Load the HT Memories
            CB_CTQual.SelectedIndex = Math.Max(0, m.HT_Intensity - 1);
            CB_CTMemory.SelectedValue = (int)m.HT_Memory;
            CB_CTVar.SelectedValue = (int)m.HT_TextVar;
            CB_CTFeel.SelectedIndex = m.HT_Feeling;
        }

        CB_Handler.Items.Clear();
        CB_Handler.Items.Add($"{Entity.OT_Name} ({TextArgs.OT})"); // OTNAME : OT

        if (!string.IsNullOrEmpty(Entity.HT_Name))
            CB_Handler.Items.Add(Entity.HT_Name);
        else
            Entity.CurrentHandler = 0;

        tabControl1.SelectedIndex = CB_Handler.SelectedIndex = Entity.CurrentHandler;

        GB_M_OT.Enabled = GB_M_CT.Enabled = GB_Residence.Enabled =
            BTN_Save.Enabled = M_Fullness.Enabled = M_Enjoyment.Enabled =
                L_Sociability.Enabled = MT_Sociability.Enabled =
                    L_Fullness.Enabled = L_Enjoyment.Enabled = !(Entity.IsEgg && Entity.IsUntraded && Entity.HT_Friendship == 0);

        if (!Entity.IsEgg)
        {
            bool enable;
            if (Entity.Generation < 6)
            {
                // Previous Generation Mon
                GB_M_OT.Text = $"{TextArgs.PastGen} {Entity.OT_Name}: {TextArgs.OT}"; // Past Gen OT : OTNAME
                GB_M_CT.Text = $"{TextArgs.MemoriesWith} {Entity.HT_Name}"; // Memories with : HTNAME
                enable = false;
                // Reset to no memory -- don't reset affection as ORAS can raise it
                CB_OTQual.SelectedIndex = CB_OTFeel.SelectedIndex = 0;
                CB_OTVar.SelectedValue = CB_OTMemory.SelectedValue = 0;
            }
            else
            {
                enable = true;
                GB_M_OT.Text = $"{TextArgs.MemoriesWith} {Entity.OT_Name} ({TextArgs.OT})"; // Memories with : OTNAME
                GB_M_CT.Text = $"{TextArgs.MemoriesWith} {Entity.HT_Name}"; // Memories with : HTNAME
                if (Entity.HT_Name.Length == 0)
                {
                    CB_Country1.Enabled = CB_Country2.Enabled = CB_Country3.Enabled = CB_Country4.Enabled =
                        CB_Region1.Enabled = CB_Region2.Enabled = CB_Region3.Enabled = CB_Region4.Enabled =
                            GB_M_CT.Enabled = false;
                    GB_M_CT.Text = $"{TextArgs.NeverLeft} {TextArgs.OT} - {TextArgs.Disabled}"; // Never Left : OT : Disabled
                }
                else
                {
                    GB_M_CT.Text = $"{TextArgs.MemoriesWith} {Entity.HT_Name}";
                }
            }
            RTB_OT.Visible = CB_OTQual.Enabled = CB_OTMemory.Enabled = CB_OTFeel.Enabled = CB_OTVar.Enabled = enable;
            M_OT_Affection.Enabled = true;
        }
        else
        {
            GB_M_OT.Text = GB_M_CT.Text = $"N/A: {GameInfo.Strings.EggName}";
        }

        init = true;

        // Manually load the Memory Parse
        RTB_CT.Text = GetMemoryString(CB_CTMemory, CB_CTVar, CB_CTQual, CB_CTFeel, Entity.HT_Name);
        RTB_OT.Text = GetMemoryString(CB_OTMemory, CB_OTVar, CB_OTQual, CB_OTFeel, Entity.OT_Name);

        // Affection no longer stored in gen8+, so only show in gen6/7.
        L_OT_Affection.Visible = L_CT_Affection.Visible = M_OT_Affection.Visible = M_CT_Affection.Visible = Entity.Format <= 7;
        L_Sociability.Visible = MT_Sociability.Visible = Entity is ISociability;
    }

    private void SaveFields()
    {
        // Save Region & Country Data
        if (Entity is IGeoTrack g)
        {
            g.Geo1_Region  = (byte)WinFormsUtil.GetIndex(CB_Region0);
            g.Geo2_Region  = (byte)WinFormsUtil.GetIndex(CB_Region1);
            g.Geo3_Region  = (byte)WinFormsUtil.GetIndex(CB_Region2);
            g.Geo4_Region  = (byte)WinFormsUtil.GetIndex(CB_Region3);
            g.Geo5_Region  = (byte)WinFormsUtil.GetIndex(CB_Region4);
            g.Geo1_Country = (byte)WinFormsUtil.GetIndex(CB_Country0);
            g.Geo2_Country = (byte)WinFormsUtil.GetIndex(CB_Country1);
            g.Geo3_Country = (byte)WinFormsUtil.GetIndex(CB_Country2);
            g.Geo4_Country = (byte)WinFormsUtil.GetIndex(CB_Country3);
            g.Geo5_Country = (byte)WinFormsUtil.GetIndex(CB_Country4);
        }

        // Save 0-255 stats
        Entity.HT_Friendship = Util.ToInt32(M_CT_Friendship.Text);
        Entity.OT_Friendship = Util.ToInt32(M_OT_Friendship.Text);

        if (Entity is IAffection a)
        {
            a.OT_Affection = (byte)Util.ToInt32(M_OT_Affection.Text);
            a.HT_Affection = (byte)Util.ToInt32(M_CT_Affection.Text);
        }
        Entity.Fullness = (byte)Util.ToInt32(M_Fullness.Text);
        Entity.Enjoyment = (byte)Util.ToInt32(M_Enjoyment.Text);

        // Save Memories
        if (Entity is ITrainerMemories m)
        {
            m.OT_Memory = (byte)WinFormsUtil.GetIndex(CB_OTMemory);
            m.OT_TextVar = CB_OTVar.Enabled ? (ushort)WinFormsUtil.GetIndex(CB_OTVar) : (ushort)0;
            m.OT_Intensity = CB_OTFeel.Enabled ? (byte)(CB_OTQual.SelectedIndex + 1) : (byte)0;
            m.OT_Feeling = CB_OTFeel.Enabled ? (byte)CB_OTFeel.SelectedIndex : (byte)0;

            m.HT_Memory = (byte)WinFormsUtil.GetIndex(CB_CTMemory);
            m.HT_TextVar = CB_CTVar.Enabled ? (ushort)WinFormsUtil.GetIndex(CB_CTVar) : (ushort)0;
            m.HT_Intensity = CB_CTFeel.Enabled ? (byte)(CB_CTQual.SelectedIndex + 1) : (byte)0;
            m.HT_Feeling = CB_CTFeel.Enabled ? (byte)CB_CTFeel.SelectedIndex : (byte)0;
        }

        if (Entity is G8PKM pk8)
            pk8.Sociability = (byte)Util.ToInt32(MT_Sociability.Text);
    }

    // Event Actions
    private void B_Save_Click(object sender, EventArgs e)
    {
        SaveFields();
        Close();
    }

    private void B_Cancel_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void GetLangStrings()
    {
        var strings = MemStrings;
        CB_OTMemory.InitializeBinding();
        CB_CTMemory.InitializeBinding();
        CB_OTMemory.DataSource = new BindingSource(strings.Memory, null);
        CB_CTMemory.DataSource = new BindingSource(strings.Memory, null);

        // Quality Chooser
        foreach (var q in strings.GetMemoryQualities())
        {
            CB_CTQual.Items.Add(q);
            CB_OTQual.Items.Add(q);
        }

        // Feeling Chooser
        foreach (var q in strings.GetMemoryFeelings(Entity.Generation))
            CB_OTFeel.Items.Add(q);
        foreach (var q in strings.GetMemoryFeelings(Entity.Format))
            CB_CTFeel.Items.Add(q);
    }

    private void UpdateMemoryDisplay(object sender)
    {
        if (sender == CB_OTMemory)
        {
            int memoryGen = Entity.Generation;
            if (memoryGen < 0)
                memoryGen = Entity.Format;

            var memory = (byte)WinFormsUtil.GetIndex((ComboBox)sender);
            var memIndex = Memories.GetMemoryArgType(memory, memoryGen);
            var argvals = MemStrings.GetArgumentStrings(memIndex, memoryGen);
            CB_OTVar.InitializeBinding();
            CB_OTVar.DataSource = new BindingSource(argvals, null);
            LOTV.Text = TextArgs.GetMemoryCategory(memIndex, memoryGen);
            LOTV.Visible = CB_OTVar.Visible = CB_OTVar.Enabled = argvals.Count > 1;
        }
        else
        {
            int memoryGen = Entity.Format;
            var memory = (byte)WinFormsUtil.GetIndex((ComboBox)sender);
            var memIndex = Memories.GetMemoryArgType(memory, memoryGen);
            var argvals = MemStrings.GetArgumentStrings(memIndex, memoryGen);
            CB_CTVar.InitializeBinding();
            CB_CTVar.DataSource = new BindingSource(argvals, null);
            LCTV.Text = TextArgs.GetMemoryCategory(memIndex, memoryGen);
            LCTV.Visible = CB_CTVar.Visible = CB_CTVar.Enabled = argvals.Count > 1;
        }
    }

    private string GetMemoryString(ComboBox m, Control arg, Control q, Control f, string tr)
    {
        string result;
        bool enabled;
        int mem = WinFormsUtil.GetIndex(m);
        if (mem == 0)
        {
            string nn = Entity.Nickname;
            result = string.Format(GameInfo.Strings.memories[0], nn);
            enabled = false;
        }
        else
        {
            string nn = Entity.Nickname;
            string a = arg.Text;
            result = string.Format(GameInfo.Strings.memories[mem], nn, tr, a, f.Text, q.Text);
            enabled = true;
        }

        // Show labels if the memory allows for them.
        if (q == CB_CTQual)
            L_CT_Quality.Visible = L_CT_Feeling.Visible = enabled;
        else
            L_OT_Quality.Visible = L_OT_Feeling.Visible = enabled;

        // Show Quality and Feeling.
        q.Visible = q.Enabled = f.Visible = f.Enabled = enabled;

        return result;
    }

    private void ChangeMemory(object sender, EventArgs e)
    {
        ComboBox m = (ComboBox)sender;
        if (m == CB_CTMemory || m == CB_OTMemory)
            UpdateMemoryDisplay(m);

        if (!init)
            return;
        RTB_OT.Text = GetMemoryString(CB_OTMemory, CB_OTVar, CB_OTQual, CB_OTFeel, Entity.OT_Name);
        RTB_CT.Text = GetMemoryString(CB_CTMemory, CB_CTVar, CB_CTQual, CB_CTFeel, Entity.HT_Name);
    }

    private void ChangeCountryIndex(object sender, EventArgs e)
    {
        int index = Array.IndexOf(PrevCountries, sender);
        int val;
        if (sender is ComboBox c && (val = WinFormsUtil.GetIndex(c)) > 0)
        {
            Main.SetCountrySubRegion(PrevRegions[index], $"sr_{val:000}");
            PrevRegions[index].Enabled = true;
        }
        else
        {
            PrevRegions[index].DataSource = new[] { new ComboItem("", 0) };
            PrevRegions[index].Enabled = false;
            PrevRegions[index].SelectedValue = 0;
        }
    }

    private void ChangeCountryText(object sender, EventArgs e)
    {
        if (sender is not ComboBox cb || !string.IsNullOrWhiteSpace(cb.Text))
            return;
        cb.SelectedValue = 0;
        ChangeCountryIndex(sender, e);
    }

    private void Update255_MTB(object sender, EventArgs e)
    {
        if (sender is not MaskedTextBox tb)
            return;
        if (Util.ToInt32(tb.Text) > byte.MaxValue)
            tb.Text = "255";
    }

    private void ClickResetLocation(object sender, EventArgs e)
    {
        Label[] senderarr = { L_Geo0, L_Geo1, L_Geo2, L_Geo3, L_Geo4 };
        int index = Array.IndexOf(senderarr, sender);
        PrevCountries[index].SelectedValue = 0;

        PrevRegions[index].InitializeBinding();
        PrevRegions[index].DataSource = new[] { new ComboItem("", 0) };
        PrevRegions[index].SelectedValue = 0;
    }

    private void B_ClearAll_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < 5; i++)
            PrevCountries[i].SelectedValue = 0;
    }

    private sealed class TextMarkup
    {
        public string Disabled { get; } = nameof(Disabled);
        public string NeverLeft { get; } = "Never left";
        public string OT { get; } = "OT";
        public string PastGen { get; } = "Past Gen";
        public string MemoriesWith { get; } = "Memories with";

        private string Species { get; } = "Species:";
        private string Area { get; } = "Area:";
        private string Item { get; } = "Item:";
        private string Move { get; } = "Move:";
        private string Location { get; } = "Location:";

        public TextMarkup(string[] args)
        {
            Array.Resize(ref args, 10);
            if (!string.IsNullOrWhiteSpace(args[0])) Disabled = args[0];
            if (!string.IsNullOrWhiteSpace(args[1])) NeverLeft = args[1];
            if (!string.IsNullOrWhiteSpace(args[2])) OT = args[2];
            if (!string.IsNullOrWhiteSpace(args[3])) PastGen = args[3];
            if (!string.IsNullOrWhiteSpace(args[4])) MemoriesWith = args[4];

            // Pokémon ; Area ; Item(s) ; Move ; Location
            if (!string.IsNullOrWhiteSpace(args[5])) Species = args[5] + ":";
            if (!string.IsNullOrWhiteSpace(args[6])) Area = args[6] + ":";
            if (!string.IsNullOrWhiteSpace(args[7])) Item = args[7] + ":";
            if (!string.IsNullOrWhiteSpace(args[8])) Move = args[8] + ":";
            if (!string.IsNullOrWhiteSpace(args[9])) Location = args[9] + ":";
        }

        public string GetMemoryCategory(MemoryArgType type, int memoryGen) => type switch
        {
            MemoryArgType.GeneralLocation => Area,
            MemoryArgType.SpecificLocation when memoryGen <= 7 => Location,
            MemoryArgType.Species => Species,
            MemoryArgType.Move => Move,
            MemoryArgType.Item => Item,
            _ => string.Empty,
        };
    }
}
