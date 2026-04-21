# 7dtd-electric-stations — CLAUDE.md

## 案件概要

- **案件名**: 7DTD Electric Stations
- **クライアント**: 個人開発（Nexus Mods / GitHub 公開）
- **ステータス**: 進行中
- **開始日**: 2026-04-21

## 要件

7 Days to Die のクラフトステーション（キャンプファイア・炉・化学ステーション）を、
燃料の代わりに電力グリッド（発電機・ソーラーパネル・バッテリー）で動作させる MOD。

- 電力が供給されている間だけクラフト可能
- 電力が切れたらクラフト停止（バニラの燃料切れと同じ挙動）
- 既存のバニラワークステーションを置き換え or 新ブロックとして追加
- 各 modlet は独立してインストール可能

## ターゲットバージョン

- **7 Days to Die**: 2.6（最新）
- **参照 DLL**: `projects/mod/_reference/7dtd-managed/Assembly-CSharp.dll`（sdtd-test より、V2.6 b14）
- **Harmony**: HarmonyX（`0_TFP_Harmony` 経由）

## 技術スタック

- **C# + HarmonyX**: 電力連動ロジック（TileEntityWorkstation × IPowered）
- **XML Modlet**: ブロック定義・レシピ・ローカライズ
- **ビルド**: .NET / MSBuild（`dotnet build`）

## ディレクトリ構成

```
7dtd-electric-stations/
├── ElectricCampfire/           電気キャンプファイア（IH コンロ風）
│   ├── ModInfo.xml
│   ├── Config/
│   │   └── blocks.xml          新ブロック定義
│   └── Harmony/
│       └── ElectricCampfire.cs Harmony パッチ
│
├── ElectricWorkstation/        電気炉 + 電気化学ステーション
│   ├── ModInfo.xml
│   ├── Config/
│   │   └── blocks.xml
│   └── Harmony/
│       └── ElectricWorkstation.cs
│
└── docs/                       設計メモ・調査結果
```

## 実装方針

### 電力連動の仕組み

```
TileEntityWorkstation（クラフトロジック）
  ↕ Harmony パッチ
IPowered / TileEntityPowered（電力グリッド）
```

- `TileEntityWorkstation.UpdateTick` にパッチを当て、電力状態を確認
- 電力なし → 燃料スロットへの消費をブロック or クラフト進行を停止
- 電力あり → 通常動作

### 参考 MOD

- `StopFuel_Terabitia`（OcbStopFuelWaste）: 燃料制御の Harmony パッチ実装例（game01 導入済み）

## メモ

- 参照 DLL は V2.6 b14（2026-04-21 更新）。DLL を差し替えた場合は両 modlet を `dotnet build` で再ビルドする
- Nexus Mods ページは modlet ごとに 1 ページ作成予定
- コード修正は必ず GitHub Issue を発行してから着手する
