# ラーニングパス「Azure Digital Twins と Unity を使用して Mixed Reality デジタル ツインを構築する」にAzure Mapsをアドオンしてみよう 
## このサンプルについて

このサンプルはラーニングパス「[Azure Digital Twins と Unity を使用して Mixed Reality デジタル ツインを構築する](https://docs.microsoft.com/ja-jp/learn/paths/build-mixed-reality-azure-digital-twins-unity/?WT.mc_id=MR-MVP-5003104)」を基にAzureで提供されているDigital twinsに関するサービスの１つ「[Azure Maps](https://docs.microsoft.com/ja-jp/azure/azure-maps/?WT.mc_id=MR-MVP-5003104)」によるデータの可視化を追加します。
Build 2021で紹介されていたDigital TwinsやMixed Reality, Metaverseといった話を一度見ておくと理解が深まります。

### 簡単な紹介
AzureにおけるDigital Twinsについては、物理環境からのセンシングから始まりデータの分析、可視化といった要件をレイヤーで分割しそれぞれをサービスで提供しています。サービス間はサーバレスのAzunre Functions等を活用しています。
このため、必要に応じて利用した要素を追加し段階的なDigital Twinsの導入もサポート可能になっています。
サービスの技術スタックは以下の通りです。
データの可視化については、「[Microsot Mesh](https://docs.microsoft.com/ja-jp/mesh/?WT.mc_id=MR-MVP-5003104)」「[Power Platform](https://docs.microsoft.com/ja-jp/power-platform/?WT.mc_id=MR-MVP-5003104)」「[Azure Maps](https://docs.microsoft.com/ja-jp/azure/azure-maps/?WT.mc_id=MR-MVP-5003104)」といったサービスを利用します。
![image1](https://github.com/TakahiroMiyaura/AzureMapsIndoorDataSamples/raw/main/images/image1.png)
ラーニングパス「[Azure Digital Twins と Unity を使用して Mixed Reality デジタル ツインを構築する](https://docs.microsoft.com/ja-jp/learn/paths/build-mixed-reality-azure-digital-twins-unity/)」では、「[Azure IoT Hub](https://docs.microsoft.com/ja-jp/azure/iot-hub/?WT.mc_id=MR-MVP-5003104)」、「[Azure Digital Twins](https://docs.microsoft.com/ja-jp/azure/digital-twins/?WT.mc_id=MR-MVP-5003104)」を活用しています。有力発電のモデルとしてエミュレートしたセンシングデータを「[Azure IoT Hub](https://docs.microsoft.com/ja-jp/azure/iot-hub/?WT.mc_id=MR-MVP-5003104)」へ送り、その情報を「[Azure Digital Twins](https://docs.microsoft.com/ja-jp/azure/digital-twins/?WT.mc_id=MR-MVP-5003104)」でモデル化し管理します。可視化については、「[Azure Digital Twins](https://docs.microsoft.com/ja-jp/azure/digital-twins/?WT.mc_id=MR-MVP-5003104)」がテレメトリ情報を「[Azure IoT Hub](https://docs.microsoft.com/ja-jp/azure/iot-hub/?WT.mc_id=MR-MVP-5003104)」から受け取るとSignalRを経由してHoloLensに同報発信される仕様です。

## このサンプルでできること 

このサンプルでは上記のアーキテクチャの中で「[Azure Digital Twins](https://docs.microsoft.com/ja-jp/azure/digital-twins/?WT.mc_id=MR-MVP-5003104)」から「[Azure Maps](https://docs.microsoft.com/ja-jp/azure/azure-maps/?WT.mc_id=MR-MVP-5003104)」への接続を実現します。これにより以下のような屋内地図を利用した情報の可視化が可能です。
![Demp](https://github.com/TakahiroMiyaura/AzureMapsIndoorDataSamples/raw/main/images/AzureMaps.gif)

### [Azure Maps](https://docs.microsoft.com/ja-jp/azure/azure-maps/?WT.mc_id=MR-MVP-5003104)について
![Azure Maps](https://docs.microsoft.com/ja-jp/azure/azure-maps/?WT.mc_id=MR-MVP-5003104)はBing Map等のマップサービスのベース機能を提供しています。簡単な認識としては「地図情報を自分で入れる地図サービス」といった形です。表示したい地図情報は利用者がオリジナルで投入することができます。
その情報を利用して指定座標付近の表示、拡大縮小、経路探索等を実現することができるサービスです。
地図情報は、GPSを基準とする広域な地図と屋内マップの大きく2種類に分かれます。今回はこのうち屋内マップを利用するためのサンプルを構築しました。

## サンプルの利用手順

このサンプルはラーニングパス「[Azure Digital Twins と Unity を使用して Mixed Reality デジタル ツインを構築する](https://docs.microsoft.com/ja-jp/learn/paths/build-mixed-reality-azure-digital-twins-unity/?WT.mc_id=MR-MVP-5003104?WT.mc_id=MR-MVP-5003104)」のアドオンとして利用できます。

以降の手順は上記ラーニングパスを一通り実施してください。

1. ローカル ファイルシステムに GitHub からリポジトリをダウンロードします。
```powershell
git clone https://github.com/TakahiroMiyaura/AzureMapsIndoorDataSamples.git
```
2. Powershellを開き、ダウンロードしたフォルダに移動します。
```powershell
cd AzureMapsIndoorDataSamples\ARM-Template
```
3. Azureのログインします。
```powershell
az login
```
3. projectname変数を設定します。この値はラーニングパスで利用したprojectnameと同じ名前を設定します。
```powershell
$projectname="myproj"
```
4.  以下のコマンドを実行しAzure MapsとMap Creatorsサービスを生成します。
```
az deployment group create -f azuremapsdeploy.bicep -g ${projectname}-rg --parameters projectName=${projectname} > ARM_deployment_out.txt
```
5. 以下のコマンドを実行しprimaryKeyの値を控えておきます。（引続きPowerShellのウィンドウは利用するため閉じないでください。もし閉じた場合は手順3.の変数設定を再度実施してください。）
```powershell
az maps account keys list --name $(az maps account list --resource-group ${projectname}-rg --query [0].name) --resource-group ${projectname}-rg
```
6. AzureMapsIndoorDataSamples\IndoorDataUploader\IndoorDataUploader.slnをVisual Studioで開きます。
7. IndoorDataUploader\Configuration.csを開き、AZURE_MAPS_SUBSCRIPTION_KEYに手順5.で控えたprimaryKeyを設定します。
8. IndoorDataUploaderをビルドし実行します。正常に完了したら、datasetid,statesetid,tilesetIdを控えておきます。
![Result](https://github.com/TakahiroMiyaura/AzureMapsIndoorDataSamples/raw/main/images/step8.png)
9. 再び、Power Shellに戻り、datasetidを設定します。値は手順8.で控えたidの値です。
```powershell
$datasetid="...."
```
1.  statesetidを設定します。値は手順8.で控えたidの値です。
```powershell
$statesetid="...."
```
11. azuremapskeyを設定します。値は手順5.で控えたidの値です。
```powershell
$azuremapskey="...."
```
12. 以下のコマンドを実行しAzure Digital TwinsからAzure Mapsにステータス変更するイベントグリッドを設定します。
```powershell
az deployment group create -f egformapsdeploy.bicep -g ${projectname}-rg --parameters projectName=${projectname} datasetid=${datasetid} statesetid=${statesetid} azuremapskey=${azuremapskey} > ARM_deployment_out.txt
```
13. Azure MapsのIndoor Mapを表示するサイトを作成します。実装についてはAzure Mapsの公式ドキュメント「[例:Indoor Maps モジュールを使用する](https://docs.microsoft.com/ja-jp/azure/azure-maps/how-to-use-indoor-module#example-use-the-indoor-maps-module)」をそのまま利用する事ができます。tilesetId,statesetIdを手順8で控えていた情報で置換えて、任意の名前で保存します。
``` html
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, user-scalable=no" />
    <title>Indoor Maps App</title>
    
    <link rel="stylesheet" href="https://atlas.microsoft.com/sdk/javascript/mapcontrol/2/atlas.min.css" type="text/css" />
    <link rel="stylesheet" href="https://atlas.microsoft.com/sdk/javascript/indoor/0.1/atlas-indoor.min.css" type="text/css"/>

    <script src="https://atlas.microsoft.com/sdk/javascript/mapcontrol/2/atlas.min.js"></script>
    <script src="https://atlas.microsoft.com/sdk/javascript/indoor/0.1/atlas-indoor.min.js"></script>
      
    <style>
      html,
      body {
        width: 100%;
        height: 100%;
        padding: 0;
        margin: 0;
      }

      #map-id {
        width: 100%;
        height: 100%;
      }
    </style>
  </head>

  <body>
    <div id="map-id"></div>
    <script>
      const subscriptionKey = "<Your Azure Maps Primary Subscription Key>";
      const tilesetId = "<your tilesetId>";
      const statesetId = "<your statesetId>";

      const map = new atlas.Map("map-id", {
        //use your facility's location
        center: [-122.13315, 47.63637],
        //or, you can use bounds: [# west, # south, # east, # north] and replace # with your Map bounds
        style: "blank",
        view: 'Auto',
        authOptions: { 
            authType: 'subscriptionKey',
            subscriptionKey: subscriptionKey
        },
        zoom: 19,
      });

      const levelControl = new atlas.control.LevelControl({
        position: "top-right",
      });

      const indoorManager = new atlas.indoor.IndoorManager(map, {
        levelControl: levelControl, //level picker
        tilesetId: tilesetId,
        statesetId: statesetId // Optional
      });

      if (statesetId.length > 0) {
        indoorManager.setDynamicStyling(true);
      }

      map.events.add("levelchanged", indoorManager, (eventData) => {
        //put code that runs after a level has been changed
        console.log("The level has changed:", eventData);
      });

      map.events.add("facilitychanged", indoorManager, (eventData) => {
        //put code that runs after a facility has been changed
        console.log("The facility has changed:", eventData);
      });
    </script>
  </body>
</html>
```

## 動作確認

ラーニングパス「[Azure Digital Twins と Unity を使用して Mixed Reality デジタル ツインを構築する](https://docs.microsoft.com/ja-jp/learn/paths/build-mixed-reality-azure-digital-twins-unity/?WT.mc_id=MR-MVP-5003104)」を利用すると風車T102の状態に応じてIndoorマップのT102のオブジェクトが赤（アラートあり）⇔緑（正常）になる事を確認することができます。同時にHoloLensでもアプリを起動しておくとそれぞれが連動していることを確認することが可能です。

確認手順は以下の通りです。
1. 作成したサイトをブラウザで表示します（Webサイトを構築しなくても動作します）。Azure Mapsサービスに正常に接続できると以下のような、風力発電所の平面地図が表示されます。
2. ブラウザを表示したまま、ラーニングパス「[Azure Digital Twins と Unity を使用して Mixed Reality デジタル ツインを構築する](https://docs.microsoft.com/ja-jp/learn/paths/build-mixed-reality-azure-digital-twins-unity/?WT.mc_id=MR-MVP-5003104)」で作成したDevice Simulatorを実行します。
3. Devices Simulatorが[Azure IoT Hub](https://docs.microsoft.com/ja-jp/azure/iot-hub/?WT.mc_id=MR-MVP-5003104)に対して情報を送信している状態で、Spaceキーを押すと風車T102の状態を正常<->異常と変更することが可能です。
4. T102を異常状態にするとIndoorマップの上のT102が赤くなることを確認します。
5. すべての確認が完了し不要であれば、Azure上に構築したリソースを削除します。

## 参考リンク

* Microsoft Build 2021 関連セション
    * [Build Opening](https://mybuild.microsoft.com/sessions/ff7c228c-3471-4ea2-90f1-4673d511c41c)
    * [Building Digital Twins, Mixed Reality and Metaverse Apps](https://mybuild.microsoft.com/sessions/348c6af7-60b3-46e5-bf09-f5a9b299dd45)
    * [Connect IoT data to HoloLens 2 with Azure Digital Twins and Unity](https://mybuild.microsoft.com/sessions/815a692f-398b-4772-ac18-c021f5116757)(ラーニングパスの解説も兼ねているのでお勧め )
* ラーニングパス
    * 「[Azure Digital Twins と Unity を使用して Mixed Reality デジタル ツインを構築する](https://docs.microsoft.com/ja-jp/learn/paths/build-mixed-reality-azure-digital-twins-unity/?WT.mc_id=MR-MVP-5003104?WT.mc_id=MR-MVP-5003104)」
* docs
    * [Azure Mapsのドキュメント](https://docs.microsoft.com/ja-jp/azure/azure-maps/?WT.mc_id=MR-MVP-5003104)
    * [Microsot Meshへようこそ](https://docs.microsoft.com/ja-jp/mesh/?WT.mc_id=MR-MVP-5003104)
    * [Microsoft Power Platformドキュメント](https://docs.microsoft.com/ja-jp/power-platform/?WT.mc_id=MR-MVP-5003104)
    * [Azure IoT Hubのドキュメント](https://docs.microsoft.com/ja-jp/azure/iot-hub/?WT.mc_id=MR-MVP-5003104)
    * [Azure Digital Twinsのドキュメント](https://docs.microsoft.com/ja-jp/azure/digital-twins/?WT.mc_id=MR-MVP-5003104)

## コンテンツについて
© 2021 Takahiro Miyaura All rights reserved.
本コンテンツの著作権、および本コンテンツ中に出てくる商標権、団体名、ロゴ、製品、サービスなどはそれぞれ、各権利保有者に帰属します