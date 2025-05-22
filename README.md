# ![Emuratch](Images/Logo.svg)

[![.NET Build](https://github.com/hayattgd/Emuratch/actions/workflows/build.yml/badge.svg?branch=dev)](https://github.com/hayattgd/Emuratch/actions/workflows/build.yml)
[![GitHub License](https://img.shields.io/github/license/hayattgd/Emuratch)](https://github.com/hayattgd/Emuratch/blob/stable/LICENSE)

Scratch emulator made with C# & [raylib](https://www.raylib.com/)

Visit [nightly.link](https://nightly.link/hayattgd/Emuratch/workflows/build/dev) for latest builds.
Emuratch supports Windows, MacOS and Linux.

For using MacOS build, please execute command below to install gtk.
```
brew install gtk+3
```

If you're using arm64 cpu, please run `Mac-fix-libgdiplus.sh` on directory where executable is located.

## Controls

| Key   | Behaviour           |
|-------|---------------------|
| F1    | Load project        |
| F2    | Fix window size     |
| F3    | Show line           |
| F5    | Press flag          |
| PAUSE | Pause / resume game |
| -     | Frame advance       |

## LICENSE

This repository is distributed under [MIT License](./LICENSE).

## Used library

| Name                                                | License                                                                                         |
|-----------------------------------------------------|-------------------------------------------------------------------------------------------------|
| [raylib-cs](https://github.com/ChrisDill/Raylib-cs) | [Zlib license](https://github.com/chrisdill/raylib-cs/blob/master/LICENSE)                      |
| [Newtonsoft.Json](https://www.newtonsoft.com/json)  | [MIT License](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)                |
| [SVG.NET](https://github.com/svg-net/SVG)           | [Microsoft Public License](https://github.com/svg-net/SVG/blob/master/license.txt)              |
| [GtkSharp](https://github.com/GtkSharp/GtkSharp)    | [GNU LIBRARY GENERAL PUBLIC LICENSE](https://github.com/GtkSharp/GtkSharp/blob/develop/LICENSE) |

(probably [Dependency graph](https://github.com/hayattgd/Emuratch/network/dependencies) has better view)
