#!/bin/bash

# 絶対参照ではなく相対パスで指定されたファイルに対して
# com.apple.quarantine 属性を削除するスクリプト

# 対象ファイルの相対パス
TARGET_RELATIVE_PATH="Assets/Tests/bin/Mac/flip"

# スクリプトのあるディレクトリを基準に絶対パスへ変換
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TARGET_PATH="$SCRIPT_DIR/$TARGET_RELATIVE_PATH"

# チェック: ファイルが存在するか
if [ ! -e "$TARGET_PATH" ]; then
  echo "対象ファイルが存在しません: $TARGET_PATH"
  exit 1
fi

# quarantine 属性を削除
echo "Quarantine 属性を削除します: $TARGET_PATH"
xattr -d com.apple.quarantine "$TARGET_PATH" 2>/dev/null

# 結果確認
if xattr "$TARGET_PATH" | grep -q "com.apple.quarantine"; then
  echo "まだ quarantine 属性が残っています（権限が必要かも）"
  echo "sudo を使ってもう一度実行してください:"
  echo "sudo $0"
  exit 1
else
  echo "quarantine 属性は正常に削除されました"
fi