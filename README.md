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
