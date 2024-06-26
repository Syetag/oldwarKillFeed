using Rocket.API;

namespace oldwar
{
    public class Config : IRocketPluginConfiguration
    {
        public ushort EffectID { get; set; }
        public string PlayerNameColor { get; set; }
        public short MinEffectKey { get; set; }
        public short MaxEffectKey { get; set; }

        public void LoadDefaults()
        {
            EffectID = 14018;
            PlayerNameColor = "FFD700";

            MinEffectKey = 0;
            MaxEffectKey = 150;
        }
    }
}