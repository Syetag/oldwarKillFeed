using Rocket.API;

namespace KillFeedPlusPlus
{
    public class Config : IRocketPluginConfiguration
    {
        public ushort EffectID { get; set; }
        public string PlayerNameColor { get; set; }
        public string MurdererNameColor { get; set; }
        public short MinEffectKey { get; set; }
        public short MaxEffectKey { get; set; }

        public void LoadDefaults()
        {
            EffectID = 14018;
            PlayerNameColor = "FFD700";
            MurdererNameColor = "FFD700";

            MinEffectKey = 0;
            MaxEffectKey = 150;
        }
    }
}