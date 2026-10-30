# Lyricod CM Helper

A small helper that sets up NetEase Cloud Music sync for [Lyricod](https://github.com/Indarkdust/Lyricod-App). Lyricod downloads it only when you choose to set this up, in Settings > General > NetEase Cloud Music sync, and runs it for you. It isn't included in Lyricod's own downloads.

NetEase Cloud Music doesn't tell Windows where it is in a song, so Lyricod has to time songs itself, and lyrics can drift after you pause or seek. Two add-ons by other developers fix that: **BetterNCM**, which loads plugins into NetEase Cloud Music, and **InfLink-rs**, a plugin that reports the song and its position to Windows.

## What it does

When you install:

1. Downloads [BetterNCM](https://github.com/std-microblock/chromatic/releases/tag/1.3.4) 1.3.4, its last release, and checks it against a fixed SHA-256.
2. Downloads the latest [InfLink-rs](https://github.com/apoint123/inflink-rs/releases/latest) and checks it against the SHA-256 GitHub lists for that file, so it's exactly what InfLink-rs's author uploaded.
3. Installs Microsoft's Visual C++ runtime (x64) if it's missing. BetterNCM needs it. The download only runs if Windows confirms Microsoft signed it.
4. Closes NetEase Cloud Music, copies BetterNCM into its folder as `msimg32.dll`, and copies InfLink-rs into BetterNCM's plugins folder (`C:\betterncm\plugins`, or the folder `BETTERNCM_PROFILE` points to). Lyricod then opens NetEase Cloud Music again.

Nothing is changed until both downloads have been checked.

When you remove, it closes NetEase Cloud Music and deletes `msimg32.dll` and `InfLink-rs.plugin`. BetterNCM's other plugins and data stay.

## Before you use it

- BetterNCM and InfLink-rs are made by other developers and change how NetEase Cloud Music runs.
- BetterNCM is no longer updated, so a NetEase Cloud Music update could stop it working. InfLink-rs has been tested with NetEase Cloud Music 3.1.20 to 3.1.41.
- Only the 64-bit NetEase Cloud Music 3.x is supported.
- Windows asks for administrator permission, because NetEase Cloud Music is installed in Program Files.
- Some antivirus software may warn about BetterNCM or this helper, since they add a file to another program's folder.

## Releasing

Raise `<Version>` in `src/Lyricod-CM-Helper/Lyricod-CM-Helper.csproj`, then run the **Release** workflow from the Actions tab. It builds the helper and drafts a release; publishing the draft creates its tag and makes it the one Lyricod downloads.

## Licences

The helper is licensed under the Apache License 2.0 (see `LICENSE`). BetterNCM and InfLink-rs are licensed under the GNU GPL v3. The helper downloads them from their own releases and doesn't include them.

---

# Lyricod CM Helper（中文）

为 [Lyricod](https://github.com/Indarkdust/Lyricod-App) 设置网易云音乐同步的小工具。只有在 Lyricod 的「设置 > 通用 > 网易云音乐同步」中选择安装时，Lyricod 才会下载并运行它，它不包含在 Lyricod 的安装包中。

它会安装第三方开发者制作的 **BetterNCM** 1.3.4（用固定的 SHA-256 校验）和最新版 **InfLink-rs**（用 GitHub 提供的 SHA-256 校验），必要时安装微软签名的 Visual C++ 运行库，然后关闭网易云音乐、复制文件，再由 Lyricod 重新打开网易云音乐。移除时会删除 `msimg32.dll` 和 `InfLink-rs.plugin`。

注意：BetterNCM 已停止更新，网易云音乐更新后可能失效；仅支持 64 位网易云音乐 3.x；需要管理员权限；部分杀毒软件可能会报警。BetterNCM 和 InfLink-rs 采用 GPL v3 许可，本工具只从它们的发布页下载，不包含它们。
