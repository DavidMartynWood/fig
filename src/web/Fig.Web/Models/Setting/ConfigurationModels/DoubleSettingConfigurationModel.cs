using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class DoubleSettingConfigurationModel : SettingConfigurationModel<double?>
{
    public DoubleSettingConfigurationModel(SettingDefinitionDataContract dataContract, SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
    }
    
    public double? ConfirmUpdatedValue { get; set; }

    protected override bool IsUpdatedSecretValueValid()
    {
        var updatedValue = UpdatedValue ?? 0;
        var confirmedValue = ConfirmUpdatedValue ?? 0;
        
        return Math.Abs(updatedValue - confirmedValue) < 0.000000000001;
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        var clone = new DoubleSettingConfigurationModel(DefinitionDataContract, parent)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}