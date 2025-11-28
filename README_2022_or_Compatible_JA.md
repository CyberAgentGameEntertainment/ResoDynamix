# ResoDynamix
このドキュメントはReso Dynamix( nity 2022 or Compatible版 )のドキュメントです。

Unity 6(Render Graph)を利用している方は下記のドキュメントにジャンプしてください。

[ドキュメント( Render Graph )](README_2022_or_Compatible.md)<br/>

## 1. 概要
ResoDynamixはベースカメラとオーバーレイカメラの描画解像度を変えることができる動的解像度変更ライブラリです。<br/>
uGUIを用いたUI描画はRender ModeにOverlayを指定することで、ベースカメラとは別の解像度で描画することができますが、このライブラリを使用するとRender ModeがOverlay以外の場合でもベースカメラとは別の解像度で描画することができます。<br/>

## 2. How To Use

### 2.1 プロジェクトへのインストール
インストールは以下の手順で行います。

1. Window > Package Manager を選択
2. 「+」ボタン > Add package from git URL を選択
3. 以下を入力してインストール
   * https://github.com/CyberAgentGameEntertainment/ResoDynamix.git?path=/Assets/ResoDynamix#compatible-mode

<p align="center">
  <img width="60%" src="https://user-images.githubusercontent.com/47441314/143533003-177a51fc-3d11-4784-b9d2-d343cc622841.png" alt="Package Manager">
</p>

あるいはPackages/manifest.jsonを開き、dependenciesブロックに以下を追記します。

```json
{
    "dependencies": {
        "jp.co.cyberagent.reso-dynamix": "https://github.com/CyberAgentGameEntertainment/ResoDynamix.git?path=/Assets/ResoDynamix#compatible-mode"
    }
}
```


* https://github.com/CyberAgentGameEntertainment/ResoDynamix.git?path=/Assets/ResoDynamix#compatible-mode

なお`No 'git' executable was found. Please install Git on your system and restart Unity`のようなメッセージが出た場合、マシンにGitをセットアップする必要がある点にご注意ください。

バージョンを更新するには上述の手順でバージョンを書き換えてください。  


### 2.2 FinalBlitDynamicResolutionImageDataをUniversal Render Pipeline Assetに追加
```ResoDynamix/Runtime/Scripts/RenderPipeline/Data/FinalBlitDynamicResolutionImageData```をUniversal Render Pipeline AssetのRenderer Listに追加してください。<br/>

<img src="Documentation/000.png" alt="FinalBlitDynamicResolutionImageDataをUniversal Render Pipeline Assetに追加">

### 2.3 Create Dynamix Resolution Image FeatureをUniversal Renderer Dataに追加
```Create Dynamix Resolution Image Feature```をシーンの描画に使っているUniversal Renderer Dataに追加してください。<br/>
<img src="Documentation/011.png" alt="Create Dynamix Resolution Image Featureを追加">

### 2.4 ResoDynamixを追加
シーンにResoDynamixコンポーネントを追加してください。なお、このコンポーネントはシーンに一つだけしか設置できません。<br/>
<img src="Documentation/001.png" alt="ResoDynamixコンポーネント">

### 2.5 ResoDynamixControllerを追加
シーンにResoDynamixControllerを追加してください。このコンポーネントはシーンに複数設置することができます。<br/>
<img src="Documentation/002.png" alt="ResoDynamixControllerコンポーネント">

### 2.6 FinalBlitDynamicResolutionImageCamera.prefabを追加
シーンに```ResoDynamix/Prefabs/FinalBlitDynamicResolutionImageCamera.prefab```を追加してください。なお、このプレハブはResoDynamixControllerと一対一である必要があります。<br/>

<img src="Documentation/003.png" alt="FinalBlitDynamicResolutionImageCamera.prefab">

### 2.7 解像度を変えたいベースカメラを指定
解像度を解像度を変えたいベースカメラをResoDynamixControllerのBaseCameraに指定してください。<br/>

<img src="Documentation/004.png" alt="ベースカメラの指定">

### 2.8 動的解像度テクスチャをシーンに反映させるカメラを指定
2.4で設置したFinalBlitDynamicResolutionImageCamera.prefabをResoDynamixControllerのFinalBlitCameraに指定してください。<br/>

<img src="Documentation/005.png" alt="FinalBlitCameraの指定">

### 2.9 ResoDynamixにResoDynamixControllerを追加
2.4で設置したResoDynamixControllerを2.3で設置したResoDynamixのControllersに追加してください。<br/>

<img src="Documentation/006.png" alt="ResoDynamixControllerの追加">

## 3. Reso Dynamixコンポーネントのパラメータ
<img src="Documentation/012.png" alt="Reso Dynamixコンポーネント">

| プロパティ名 | 説明 |
| ---- | ---- |
|Controllers| シーンに設置されているReso Dynamix Controller。<br/>シーンに設置されているコントローラーは全てここに登録されている必要があります。|

## 4. Reso Dynamix Controllerコンポーネントのパラメータ
<img src="Documentation/013.png" alt="Reso Dynamix Controllerコンポーネント">

| プロパティ名 | 説明 |
| ---- | ---- |
| Base Camera Render Scale | Base Cameraのレンダリングスケール。<br/>この数値を変更することでBase Cameraのレンダリング解像度を変更することができます。Render Scaleが1.0になっている場合はReso Dynamixによる処理はスキップされます。 |
| Result Render Scale | 最終的に生成されるハイブリッドイメージのレンダリングスケール。<br/>副次的にこの解像度を変更することでUIの解像度を変更することもできます。<br/> |
| Base Camera | ベースカメラ |
| Final Blit Camera | 生成されたハイブリッド解像度のイメージをフレームバッファにBlitするカメラです。<br/> [【関連リンク】FinalBlitDynamicResolutionImageCamera.prefabを追加](#2.6)|
| Use Depth Texture With Overlay Camera| オーバーレイカメラの描画で深度テクスチャを利用するかどうかのチェックボックス。 <br/> オーバーレイカメラで深度テストやステンシルマスクを利用したい場合はこのチェックをオンにしてください。|