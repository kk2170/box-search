# Box Search

[English](README.md) | [日本語](README.ja.md)

Box Search は、アイテムがどの箱に入っているかを素早く見つけることに特化した Core Keeper 用 mod プロジェクトです。

## スタック

- 言語: C#
- Modding framework: BepInEx + Harmony

## 現在の状態

プロジェクトの初期セットアップは完了しています。

- ローカル Git リポジトリを初期化済み
- MIT ライセンスを追加済み
- Agent 開発ガイドを追加済み

初期実装も追加済みです。

- `src/BoxSearch` 配下に BepInEx/Harmony 用の C# プロジェクトを追加
- インメモリのストレージスナップショットとアイテム名検索サービスを追加
- 最小構成のゲーム内 IMGUI 検索オーバーレイを追加
- 実際の Core Keeper ストレージフック接続前でも UI と検索ループを確認できるよう、デバッグ用サンプルデータ hotkey を追加

## 予定機能

- 発見済みストレージ全体からアイテム名を検索
- 一致した箱の場所とアイテム数を表示
- UI の手間が少ない高速なゲーム内検索
- 観測レイヤーを Core Keeper の実際のストレージ/コンテナイベントに接続

## 開発

このリポジトリはクリーンな土台から始まっており、実装は `AGENT.md` のルールに従います。特に `public` クラスと `public` メソッドには XML ドキュメントコメントが必須です。

ビルド設定:

- Core Keeper のインストール先が既定の Steam パスではない場合は、`Config.Build.user.props.template` を `Config.Build.user.props` としてコピーして設定してください。
- ビルドは `Core Keeper/` 配下を想定し、生成した DLL を `BepInEx/plugins/` に配置します。

現在のランタイム操作:

- `Ctrl+F`: 検索オーバーレイの表示/非表示
- `Esc`: 検索オーバーレイを閉じる
- 任意のデバッグ操作: BepInEx 設定で `Debug/EnableDebugSampleHotkey` を有効にし、`F8` を押すとサンプルのコンテナデータを投入

## ライセンス

MIT License の下で公開しています。詳細は `LICENSE` を参照してください。
