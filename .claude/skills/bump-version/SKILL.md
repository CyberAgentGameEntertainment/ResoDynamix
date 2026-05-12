---
name: bump-version
description: "ResoDynamix パッケージのバージョンをバンプする。バンプ種別（major/minor/patch）をインタラクティブに選択して実行"
---

# ResoDynamix バージョンバンプスキル

`Assets/ResoDynamix/package.json` のバージョンをバンプし、`bump-version` ブランチで push したうえでタグを作成する。

ResoDynamix は **サブモジュールを含まない単独パッケージ** なので、SIRIUS のような複数パッケージ／サブモジュール処理は行わない。

## 呼び出し方

```
/bump-version
```

---

## 対象パッケージ（固定）

| パッケージ | package.json パス |
|-----------|------------------|
| Reso Dynamix | `Assets/ResoDynamix/package.json` |

---

## 実行フロー

全ての git コマンドは ResoDynamix リポジトリのルート (`d:/ResoDynamix`) で実行する。

### Step 1: 現在のバージョン確認

1. `Assets/ResoDynamix/package.json` を Read ツールで読み込む
2. `"version"` フィールドが semver 形式（`X.Y.Z`）であることを検証する
3. 現在のバージョンを記憶する

### Step 2: 事前チェック

`package.json` に以下のフィールドが揃っているか検証する:

- `name` — `jp.co.cyberagent.reso-dynamix` であること
- `displayName` — 存在すること
- `version` — semver 形式（`X.Y.Z`）であること
- `unity` — 存在すること
- `license` — 存在すること
- `author` — 存在すること

不足・不正なフィールドがあれば警告を表示し、AskUserQuestion で続行/中止を確認する。

### Step 3: バンプ種別の選択

AskUserQuestion でバンプ種別を確認する:

- **メジャーバンプ**: `X.Y.Z` → `X+1.0.0`（例: `1.0.0` → `2.0.0`）
- **マイナーバンプ**: `X.Y.Z` → `X.Y+1.0`（例: `1.0.0` → `1.1.0`）
- **パッチバンプ**: `X.Y.Z` → `X.Y.Z+1`（例: `1.0.0` → `1.0.1`）
- **キャンセル**

新バージョンを算出する。

### Step 4: 確認

以下を表示し AskUserQuestion で確認する:

```
バンプ対象:
  - Reso Dynamix: X.Y.Z → X'.Y'.Z'
```

- **バンプする**: 実行
- **キャンセル**: 処理を中止

### Step 5: ブランチ作成

1. 作業ツリーがクリーンであることを確認する:
   ```bash
   git status --porcelain
   ```
   - 未コミットの変更がある場合はエラーを表示して中止する

2. `bump-version` ブランチが既に存在しないか確認し、作成する:
   ```bash
   git branch --list bump-version
   ```
   - 出力が非空（既に存在する）→ エラーを表示して処理を中止し、ユーザーに対処を案内する:
     ```
     エラー: bump-version ブランチが既に存在します。
     不要であれば削除してから再実行してください:
       git branch -d bump-version
     ```
   - 出力が空 → ブランチを作成する:
     ```bash
     git checkout -b bump-version
     ```

### Step 6: package.json の更新

`Assets/ResoDynamix/package.json` の `"version"` フィールドを新バージョンに Edit ツールで書き換える（フォーマット維持）。更新後に内容を確認する。

### Step 7: commit & push

```bash
git add Assets/ResoDynamix/package.json
git commit -m "chore: bump version to X.Y.Z"
git push origin bump-version
```

`package.json` 以外のファイルを add してはならない。

### Step 8: タグの作成と push

```bash
git tag vX.Y.Z
git push origin vX.Y.Z
```

### Step 9: 完了報告

以下の情報を表示して完了:

- 変更前のバージョン → 変更後のバージョン
- push 先ブランチ: `bump-version`
- 作成されたタグ: `vX.Y.Z`
- 次のアクション案内: GitHub 上で `bump-version` → `main` の PR を作成する

---

## 注意事項

- ResoDynamix はサブモジュールを持たない単独リポジトリなので、`git -C <submodule>` 形式は使わない
- タグのフォーマットは `vX.Y.Z`（例: `v1.1.0`）
- `compatible-mode` ブランチ用のバンプは対象外。`main` ブランチでのみ実行することを想定する
- push やタグの push が失敗した場合はエラーを表示してユーザーに対処を案内する
- ブランチ作成前に作業ツリーがクリーンであることを必ず確認する
