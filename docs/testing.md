# テスト手順

## ElectricCampfire — game01 での動作確認

### 前提

- game01 に SSH 接続できること: `ssh -i ~/.ssh/private_infra root@162.43.7.73`
- game01 の `0_TFP_Harmony` が導入済み（確認済み）

### 1. DLL ビルド

```bash
cd projects/mod/7dtd-electric-stations/ElectricCampfire
/home/vscode/.dotnet/dotnet build ElectricCampfire.csproj
# → bin/ElectricCampfire.dll が生成される
```

### 2. game01 へ転送

```bash
# modlet フォルダごと転送（DLL + Config を含む）
ssh -i ~/.ssh/private_infra root@162.43.7.73 "mkdir -p /home/sdtd/7dtd_server/Mods/ElectricCampfire/Config"

scp -i ~/.ssh/private_infra \
  ElectricCampfire/ModInfo.xml \
  root@162.43.7.73:/home/sdtd/7dtd_server/Mods/ElectricCampfire/

scp -i ~/.ssh/private_infra \
  ElectricCampfire/bin/ElectricCampfire.dll \
  root@162.43.7.73:/home/sdtd/7dtd_server/Mods/ElectricCampfire/

scp -i ~/.ssh/private_infra \
  ElectricCampfire/Config/blocks.xml \
  ElectricCampfire/Config/Localization.txt \
  root@162.43.7.73:/home/sdtd/7dtd_server/Mods/ElectricCampfire/Config/
```

### 3. サーバー再起動

```bash
ssh -i ~/.ssh/private_infra root@162.43.7.73 "systemctl restart 7dtd || service 7dtd restart"
# またはサーバー管理方法に従って再起動
```

### 4. ゲーム内確認チェックリスト

- [ ] クリエイティブメニューに「Electric Campfire」が表示される
- [ ] ブロックを設置するとモダンオーブンのモデルが表示される
- [ ] 右クリック → ワークステーション UI が開く（燃料スロットなし）
- [ ] 食材を入れてレシピが進行する（IsBurning=true で動作）
- [ ] サーバーログに `[ElectricCampfire] Harmony patches applied.` が出る

### 5. ログ確認

```bash
ssh -i ~/.ssh/private_infra root@162.43.7.73 \
  "grep -i 'ElectricCampfire\|electric' /home/sdtd/.local/share/7DaysToDie/Saves/*/server*/output_log*.txt 2>/dev/null | tail -20"
```

---

## ElectricWorkstation — game01 での動作確認

### 1. DLL ビルド

```bash
cd projects/mod/7dtd-electric-stations/ElectricWorkstation
/home/vscode/.dotnet/dotnet build ElectricWorkstation.csproj
# → bin/ElectricWorkstation.dll が生成される
```

### 2. game01 へ転送

```bash
ssh -i ~/.ssh/private_infra root@162.43.7.73 "mkdir -p /home/sdtd/7dtd_server/Mods/ElectricWorkstation/Config"

scp -i ~/.ssh/private_infra \
  ElectricWorkstation/ModInfo.xml \
  root@162.43.7.73:/home/sdtd/7dtd_server/Mods/ElectricWorkstation/

scp -i ~/.ssh/private_infra \
  ElectricWorkstation/bin/ElectricWorkstation.dll \
  root@162.43.7.73:/home/sdtd/7dtd_server/Mods/ElectricWorkstation/

scp -i ~/.ssh/private_infra \
  ElectricWorkstation/Config/blocks.xml \
  ElectricWorkstation/Config/Localization.txt \
  root@162.43.7.73:/home/sdtd/7dtd_server/Mods/ElectricWorkstation/Config/
```

### 3. サーバー再起動

```bash
ssh -i ~/.ssh/private_infra root@162.43.7.73 "systemctl restart 7dtd || service 7dtd restart"
```

### 4. ゲーム内確認チェックリスト

#### Electric Forge
- [ ] クリエイティブメニューに「Electric Forge」が表示される
- [ ] ブロックを設置すると炉のモデルが表示される
- [ ] 右クリック → 炉 UI が開く
- [ ] 鉄鉱石などを入れると燃料なしで製錬が進む
- [ ] サーバーログに `[ElectricWorkstation] Harmony patches applied.` が出る

#### Electric Chemistry Station
- [ ] クリエイティブメニューに「Electric Chemistry Station」が表示される
- [ ] ブロックを設置すると化学ステーションのモデルが表示される
- [ ] 右クリック → ワークステーション UI が開く（燃料スロットなし）
- [ ] 材料を入れてレシピが進行する（IsBurning=true で動作）

### 5. ログ確認

```bash
ssh -i ~/.ssh/private_infra root@162.43.7.73 \
  "grep -i 'ElectricWorkstation\|electric' /home/sdtd/.local/share/7DaysToDie/Saves/*/server*/output_log*.txt 2>/dev/null | tail -20"
```

### トラブルシューティング

| 症状 | 対処 |
|------|------|
| 炉が動かない（製錬が進まない） | ログで `TileEntityForge patch error` を確認。`fuelInForgeInTicks` フィールド名が変わった可能性 |
| 化学ステーションが動かない | ブロック名 `workstationChemistryStation` が正しいか確認（バージョンにより異なる可能性） |
| DLL ロードエラー | `0_TFP_Harmony` が導入済みか確認 |

---

## 既知の制限（Phase 1）

- 電力グリッム不要で常時動作する（電力接続のチェックなし）
- Phase 2 で `PowerManager` 連携を実装予定（Issue #TBD）

## トラブルシューティング

| 症状 | 対処 |
|------|------|
| ブロックが表示されない | `blocks.xml` の xpath・block 名を確認 |
| ワークステーション UI が開かない | `Class = "Campfire"` が正しく継承されているか確認 |
| 料理が進まない | ログで Harmony パッチエラーを確認 |
| モデルが表示されない | `stoveElectricModern_01Prefab.prefab` パスを確認（バージョン差異の可能性） |
