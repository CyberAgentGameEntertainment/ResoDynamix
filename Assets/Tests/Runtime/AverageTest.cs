using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TestHelper.Attributes;
using Tests.Runtime.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Graphics;



namespace Tests.Runtime
{
    /// <summary>
    /// アベレージテストを行うクラス
    /// Jzazbz色空間でのピクセルの差分の平均値を使ってテストを行います。
    /// </summary>
    public class AverageTest
    {
        [TestCase("Demo01_Standard", ExpectedResult = (IEnumerator)null)]
        [TestCase("Demo02_UseResultRenderScale", ExpectedResult = (IEnumerator)null)]
        [TestCase("Demo03_UseSpriteMask", ExpectedResult = (IEnumerator)null)]
        [TestCase("Demo04_MultiCamera", ExpectedResult = (IEnumerator)null)]
        [GameViewResolution(1920, 1080, "Full HD")]
        public IEnumerator Test(string scenePath)
        {
            yield return TestUtility.LoadScene($"Assets/Demo/{scenePath}.unity");
            
            var settings = new ImageComparisonSettings
            {
                TargetWidth = Screen.width,
                TargetHeight = Screen.height,
                AverageCorrectnessThreshold = 0.01f,
                PerPixelCorrectnessThreshold = 0.005f,
                IncorrectPixelsThreshold = 0.1f
            };
            // このタイミングでスクリーンショットを撮ればシーンの描画結果と一致するらしい
            var screenshotSrc = ScreenCapture.CaptureScreenshotAsTexture();
            var expected = TestUtility.ExpectedImage();
            // 成功イメージのフォーマットに合わせて再作成する。
            var screenshot = new Texture2D(expected.width, expected.height, expected.format, false);
            screenshot.SetPixels(screenshotSrc.GetPixels());
            screenshot.Apply();
            // Flipを使った画像比較
            ImageAssertExtensions.AreEqualWithFlip(screenshot, settings);
        }

        [MenuItem("Window/ResoDynamix/Test/Copy AverageTest Result")]
        static void CopyResult()
        {
            string[] platforms = { @"WindowsEditor/Direct3D11", @"OSXEditor_AppleSilicon/Metal"};
            foreach(var platform in platforms)
            {
                // コピー元とコピー先のパスを定義
                var sourceDirectory = $"Assets/ActualImages/Linear/{platform}/None";
                var destinationDirectory = $"Assets/Tests/SuccessfulImages/Linear/{platform}/None";

                if (!Directory.Exists(sourceDirectory))
                {
                    continue;
                }
                // コピー先のディレクトリが存在しない場合は作成
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                // コピー元ディレクトリからファイルを取得し、条件に合うものをコピー
                var files = Directory.EnumerateFiles(sourceDirectory, "*.png")
                    .Where(file => !file.EndsWith(".diff.png", StringComparison.OrdinalIgnoreCase) 
                                   && !file.EndsWith(".expected.png", StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    // ファイル名を取得
                    string fileName = Path.GetFileName(file);

                    // コピー先のパスを定義
                    string destFile = Path.Combine(destinationDirectory, fileName);

                    // ファイルをコピー
                    File.Copy(file, destFile, overwrite: true);
                }
            }
        }
    }
}
