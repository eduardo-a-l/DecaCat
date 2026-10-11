# DecaCat


## Building

In the Unity editor use the **DecaCat > Build** menu:

- **Windows** builds `Builds/Windows` and a ready to share `.zip`
- **Android APK** builds `Builds/Android/DecaCat-v<version>.apk` to install on a phone
- **Android App Bundle** builds the `.aab` for Google Play
- **Windows + Android APK** builds both

From the command line (close the editor first) run `build.bat windows`, `build.bat apk`, `build.bat aab` or `build.bat all`. Set `UNITY_EXE` if Unity is not in the default Hub folder.

Requirements: Unity 6000.4.10f1 with the Windows Build Support and Android Build Support (including OpenJDK and Android SDK and NDK Tools) modules.

The app icon is generated from the player cat sprite, see `Assets/Art/Icon`.

For Google Play, create a keystore and set `DECACAT_KEYSTORE`, `DECACAT_KEYSTORE_PASS`, `DECACAT_KEY_ALIAS` and `DECACAT_KEY_PASS` before building the App Bundle. Without them the build is signed with the debug key, which is fine for installing the APK on a phone.

Increase `bundleVersion` and the Android bundle version code in Player Settings for every release.

## Floor progression

Floors are grouped in sets of 5. There are 6 groups per cycle (floors 1-30), each with its own color: Blue, Yellow, Orange, Red, Green, Purple, in that order for the first cycle. Every later cycle uses the 6 colors in a random order that is saved with the run, and its first color is never the last color of the previous cycle.

Everything is edited in `Assets/Resources/FloorThemes.asset`:

- **Themes**: one entry per color, with the floor and wall tint, 5 new enemies and 3 bosses.
- **Enemies**: drag the enemy prefab into a slot. Unlock floor (1 to 5) is the first floor of the group where it can appear. Enemies from earlier groups of the current cycle keep appearing less often. Empty slots are skipped.
- **Bosses**: the 3 bosses of a color are spread so each one shows up at least once in the 5 floors, and floor 1 always has the first boss of Blue (Slime King). Bosses of other colors never show up until the cycle loops. An empty slot reuses another boss of the same color, and with none set the boss room layout's own boss is used.
- **Tuning**: how much stronger enemies get every floor and every group of 5 floors (health, speed, and contact damage every few groups).

Room layouts use the floor's enemies by default. Untick `Use Floor Enemies` on a layout to keep its own enemy list, as the Ghost room does. The number of rooms per floor is in `Assets/Data/Floors/Floor 1.asset`.

## Index

The Index (main menu and pause menu) lists items, enemies and bosses per save slot. Entries stay as `???` until the player equips or sees an item, or enters a room with that enemy. Bare Hands is always listed and has no icon.

- Item descriptions are the `Description` field on each item asset.
- Enemy and boss entries come from `Assets/Resources/FloorThemes.asset`. Set `Display Name` and `Description` on the enemy prefab, or leave them empty to use the built-in text.
- A new boss class should override `IsBoss` so it shows under Bosses.

## Room designs

Room layouts are text grids in `Assets/Data/Rooms` and are picked per room type from `Assets/Data/Floors/Floor 1.asset` (start, normal, power and boss pools). All layouts must be 18x10 with the 2 cell wide doors in the middle of each side.

## Powers

Power rooms (2 to 3 per floor, set in `Floor 1.asset`) can be any room except the start and boss rooms and are spread apart from each other. They hold 1 or 2 altars, each with a random power. Walk up to one and press E (the E button on phones) to take it, and the other altar in that room disappears. Taking a power replaces the one you have. Every power gives a passive, a negative, and a special on Space (the power button on phones). A power has 10 charges, one per special use, and it is lost when they run out. Your player sprite changes to the power's variation but the hitbox stays the same.

- Power names, texts, icons, player sprites and accent colors are in `Assets/Resources/Powers.asset`.
- All numbers (durations, chances, damage, cooldowns) are constants at the top of each class in `Assets/Scripts/Powers`, for example `IcePower.cs`.
- Put an `A` in a room layout where an altar should be. Power rooms use the `PowerRoom`, `AlcovePower` and `PillarPower` layouts.
- Enemy attacks can have an element (`AttackElement` on `Enemy`), which Ice and Fire use for their 80% evade.
- Walls inside a room are on the `Obstacle` physics layer and the room border is on `RoomWall`. Bosses (any enemy with `IsBoss`) walk over obstacles, and the Ghost power and ghost enemies pass through them.

Every room except the boss room that has no crate in its layout has a 50% chance of getting one bonus crate. The crate used for that is set in `FloorThemes.asset` (`Default Crate Prefab`).
