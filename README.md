## oldwarKillfeed - First-class Killfeed for your server!

**A fully working and beautifully designed KillFeed.**

**In the plugin config, you can change:**
- EffectID = 14018;
- PlayerNameColor = "FFD700";
- MurdererNameColor = "FFD700";
  
This range is needed so that the effect key is not generated in it:
- MinEffectKey = 0;
- MaxEffectKey = 150;

**REQUIRED ❗❗❗**
**UI - [Steam Workshop Link](https://steamcommunity.com/sharedfiles/filedetails/?id=3276043866)**

## Features

- **When a player dies, a KillFeed entry is shown to all players!:**
![ezgif com-video-to-gif-converter](https://github.com/Syetag/oldwarKillFeed/assets/109528894/935a0b7b-6016-4238-a410-9521efab3ec9)

- **Any player's death is logged, even death from acid, cold or from another player!:**
![ezgif com-video-to-gif-converter (1)](https://github.com/Syetag/oldwarKillFeed/assets/109528894/5258f8c8-e41f-4fa7-a611-5be18027b54f)
![ezgif com-video-to-gif-converter (2)](https://github.com/Syetag/oldwarKillFeed/assets/109528894/5fa31f2b-9c0d-4021-bacb-d59e62cfea26)


## Important
        public override TranslationList DefaultTranslations => new TranslationList()
        {
            { "UnknownPlayer", "Unknown" },
            { "UnknownMurderer", "Unknown" },
            { "UnknownWeapon", "Unknown" },
            { "DeathCause_Bleeding", "bled out!" },
            { "DeathCause_Bones", "broke their bones!" },
            { "DeathCause_Freezing", "froze to death!" },
            { "DeathCause_Burning", "burned to death!" },
            { "DeathCause_Food", "starved to death!" },
            { "DeathCause_Water", "died of thirst!" },
            { "DeathCause_Gun_1", "killed" },
            { "DeathCause_Gun_2", "with" },
            { "DeathCause_Melee_1", "killed" },
            { "DeathCause_Melee_2", "with" },
            { "DeathCause_Zombie", "was killed by a zombie!" },
            { "DeathCause_Animal", "was killed by an animal!" },
            { "DeathCause_Suicide", "commited suicide!" },
            { "DeathCause_Infection", "died from infection!" },
            { "DeathCause_Punch", "punched" },
            { "DeathCause_Breath", "suffocated!" },
            { "DeathCause_Vehicle", "was hit by a vehicle!" },
            { "DeathCause_Roadkill", "died in a car accident!" },
            { "DeathCause_Grenade", "blew themselves up with a grenade!" },
            { "DeathCause_Shred", "was shredded!" },
            { "DeathCause_Landmine", "stepped on a landmine!" },
            { "DeathCause_Arena_1", "defeated" },
            { "DeathCause_Arena_2", "in the arena!" },
            { "DeathCause_Missile", "was hit by a missile!" },
            { "DeathCause_Charge", "was electrocuted!" },
            { "DeathCause_Splash", "died from an explosion!" },
            { "DeathCause_Sentry", "was killed by a sentry!" },
            { "DeathCause_Acid", "dissolved in acid!" },
            { "DeathCause_Boulder", "was crushed!" },
            { "DeathCause_Burner", "burned alive!" },
            { "DeathCause_Spit", "was poisoned!" },
            { "DeathCause_Default", "died!" }
        };

**Each entry in KillFeed lasts 5 seconds, and their maximum number is 5 units.**
