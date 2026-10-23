using System;
using System.Xml.Linq;
using Common.Utilities;

namespace Common.Resources.Xml.Descriptors;

public class EquipmentBoost : ItemData {
    public EquipmentBoost(XElement e, ItemData parent = null, byte parentField = 0) {
        SetParent(parent, parentField);
        if (e == null) // Null when instance by itemdata import
        {
            _initialized = true;
            return;
        }

        Stat = ParseStat(e.GetAttribute<string>("stat"));
        Amount = e.GetAttribute<int>("amount");
        LevelIncrease = e.HasElement("LevelIncrease")
            ? new LevelIncreaseDesc(e.Element("LevelIncrease"), this, (byte)EquipmentBoostField.LevelIncrease)
            : null;

        _initialized = true;
    }

    // Newer game data names the stat (stat="DEF"); older data used the stat number (stat="21").
    // Numbers below are the classic stat ids, checked against items present in both versions.
    private static int ParseStat(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return 0;
        }

        if (int.TryParse(value, out var number)) {
            return number;
        }

        switch (value.Trim().ToUpperInvariant()) {
            case "MAXHP": return 0;
            case "MAXMP": return 3;
            case "XP": return 6;
            case "ATT": return 20;
            case "DEF": return 21;
            case "SPD": return 22;
            case "VIT": return 26;
            case "WIS": return 27;
            case "DEX": return 28;
            default:
                XmlLoadReport.Unknown("EquipmentBoost stat", value);
                return 0;
        }
    }

    public override Type FieldsEnum => typeof(EquipmentBoostField);

    public int Stat {
        get => GetValue<int>(0);
        set => SetValue(0, value);
    }

    public int Amount {
        get => GetValue<int>(1);
        set => SetValue(1, value);
    }

    public LevelIncreaseDesc LevelIncrease {
        get => GetValue<LevelIncreaseDesc>(2);
        set => SetValue(2, value);
    }
}