---
name: bump-version
description: "ResoDynamix パッケージのバージョンをバンプする。バンプ種別（major/minor/patch）をインタラクティブに選択して実行"
---

# ResoDynamix バージョンバンプスキル

`Assets/ResoDynamix/package.json` のバージョンをバンプし、`bump-version` ブランチで push して PR 作成までを行う。
タグ作成は **PR が main に merge された後** に別途実施する（後述）。

ResoDynamix は **サブモジュールを含まない単独パッケージ** なので、SIRIUS のような複数パッケージ／サブモジュール処理は行わない。

## 呼び出し方

```
/bump-version
```

引数として `tag` を渡すと **Phase B（PR merge 後のタグ付け）** モードで動作する。

```
/bump-version tag
```

---

## 対象パッケージ（固定）

| パッケージ | package.json パス |
|-----------|------------------|
| Reso Dynamix | `Assets/ResoDynamix/package.json` |

---

## Phase A: バージョンバンプ PR の作成

引数なしで呼ばれた場合のフロー。全ての git コマンドは ResoDynamix リポジトリのルート (`d:/ResoDynamix`) で実行する。

### Step 1: 事前状態の検証

以下を順番に検証し、ひとつでも失敗したらエラー内容を表示して処理を中止する。

1. **現在ブランチが `main` であること**:
   ```bash
   git rev-parse --abbrev-ref HEAD
   ```
   `main` 以外であればエラーを表示して中止する:
   ```
   エラー: 現在ブランチが <branch> です。main に切り替えてから再実行してください:
     git switch main
   ```

2. **作業ツリーがクリーンであること**:
   ```bash
   git status --porcelain
   ```
   出力が非空ならエラー:
   ```
   エラー: 未コミットの変更があります。コミット／stash してから再実行してください。
   ```

3. **`origin/main` を最新化し、ローカル main と一致していること**:
   ```bash
   git fetch origin main
   git rev-parse HEAD
   git rev-parse origin/main
   ```
   両者が一致しない場合は fast-forward を試みる:
   ```bash
   git pull --ff-only origin main
   ```
   fast-forward できない（ローカル先行・分岐がある）場合はエラーを表示して中止する:
   ```
   エラー: ローカル main が origin/main と分岐しています。状態を整えてから再実行してください。
   ```

4. **`bump-version` ブランチがローカル／リモート双方に存在しないこと**:
   ```bash
   git branch --list bump-version
   git ls-remote --heads origin bump-version
   ```
   いずれかで出力が非空ならエラーを表示して中止する:
   ```
   エラー: bump-version ブランチが既に存在します。
   ローカル:  git branch -d bump-version
   リモート:  git push origin --delete bump-version
   ```

### Step 2: 現在のバージョンと必須フィールドの確認

1. `Assets/ResoDynamix/package.json` を Read ツールで読み込む
2. 以下のフィールドが揃っているか検証する:
   - `name` — `jp.co.cyberagent.reso-dynamix` であること
   - `displayName` — 存在すること
   - `version` — semver 形式（`X.Y.Z`）であること
   - `unity` — 存在すること
   - `license` — 存在すること
   - `author` — 存在すること
3. 不足・不正なフィールドがあれば警告を表示し、AskUserQuestion で続行/中止を確認する
4. 現在のバージョンを記憶する

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

```bash
git checkout -b bump-version
```

### Step 6: package.json の更新

`Assets/ResoDynamix/package.json` の `"version"` フィールドを新バージョンに Edit ツールで書き換える（フォーマット維持）。更新後に内容を確認する。

### Step 7: commit & push

```bash
git add Assets/ResoDynamix/package.json
git commit -m "chore: bump version to X.Y.Z"
git push -u origin bump-version
```

`package.json` 以外のファイルを add してはならない。

### Step 8: PR 作成

`gh` で `bump-version` → `main` の PR を作成する:

```bash
gh pr create --base main --head bump-version \
  --title "chore: bump version to X.Y.Z" \
  --body "Bump Reso Dynamix package version: X.Y.Z → X'.Y'.Z'"
```

### Step 9: Phase A 完了報告

以下を表示する:

- 変更前のバージョン → 変更後のバージョン
- push 先ブランチ: `bump-version`
- 作成された PR URL
- **次のアクション**: PR を main に merge した後、`/bump-version tag` を実行してタグを作成すること

> **タグはこの段階では作成しない**。squash merge / rebase / 追加修正により bump-version 上のコミットと main 上のリリースコミットが乖離する可能性があるため、タグは main の merge commit に対して打つ必要がある。

---

## Phase B: PR merge 後のタグ作成

`/bump-version tag` で呼ばれた場合のフロー。

### Step B-1: 事前状態の検証

1. **現在ブランチが `main` であること**:
   ```bash
   git rev-parse --abbrev-ref HEAD
   ```
   `main` でなければエラーを表示して中止する。

2. **`origin/main` を fetch & fast-forward pull**:
   ```bash
   git fetch origin main
   git pull --ff-only origin main
   ```

3. **作業ツリーがクリーンであること**:
   ```bash
   git status --porcelain
   ```

### Step B-2: タグ対象バージョンの確定

1. `Assets/ResoDynamix/package.json` を Read し、現在の `"version"` を取得する（これがリリース対象バージョン）
2. **直近の main 上で `package.json` の `"version"` を変更したコミットを特定する**:
   ```bash
   git log -1 --format=%H -- Assets/ResoDynamix/package.json
   ```
   そのコミットの差分で `"version"` 行が変更されていることを確認する:
   ```bash
   git show <hash> -- Assets/ResoDynamix/package.json
   ```
   `"version"` を変更していない（無関係の編集だった）場合はエラーを表示し、ユーザーに対象コミットを手動指定するよう案内する。
3. 同名タグ (`vX.Y.Z`) が既に存在しないか確認する:
   ```bash
   git tag --list vX.Y.Z
   git ls-remote --tags origin vX.Y.Z
   ```
   いずれかで存在すればエラーを表示して中止する。

### Step B-3: 確認

AskUserQuestion で確認する:

```
以下のコミットにタグ vX.Y.Z を作成します:
  <hash> <subject>

- タグを作成する
- キャンセル
```

### Step B-4: タグ作成と push

```bash
git tag vX.Y.Z <hash>
git push origin vX.Y.Z
```

### Step B-5: Phase B 完了報告

- 作成されたタグ: `vX.Y.Z`
- タグが指すコミット: `<hash>`
- push 先: `origin`

---

## 注意事項

- ResoDynamix はサブモジュールを持たない単独リポジトリなので、`git -C <submodule>` 形式は使わない
- タグのフォーマットは `vX.Y.Z`（例: `v1.1.0`）
- `compatible-mode` ブランチ用のバンプは対象外。`main` ブランチでのみ実行する
- Phase A はタグを作成しない。タグは Phase B（PR merge 後）でのみ作成する
- push やタグの push が失敗した場合はエラーを表示してユーザーに対処を案内する
