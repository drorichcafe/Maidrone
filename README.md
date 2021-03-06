
# CM3D2.Maidrone.Plugin

空撮用のドローンを召喚します。

#### アルゴリズム

| 名前           | 概要                                                 |
|:---------------|:-----------------------------------------------------|
| マニュアル     | キー操作でドローンを動かします                       |
| ウェイポイント | 設定したルートに沿ってドローンが動きます             |
| リサージュ     | パラメータで設定された振動曲線上をドローンが動きます |

#### コンフィグ

ConfigフォルダにあるMaidrone.xmlを編集して設定を記述してください。
コンフィグファイルは召喚する度にロードされるので本体の再起動の必要はありません。

#### 更新履歴

* 0.0.0.7
  * ジョイスティック入力に対応

* 0.0.0.6
  * リサージュアルゴリズムにメイドフォーカス機能を追加

* 0.0.0.5
  * 画角を一人称視点と三人称視点で別々に設定できるように修正

* 0.0.0.4
  * ダンスシーンでスクリーンセーバーが起動しないように修正
  * カメラの画角を設定するパラメータを修正
  * マニュアル操作に停止キーを追加
  * 一人称視点でプロペラが表示されてしまうことがある不具合を修正

* 0.0.0.3
  * ドローンが存在しているときマニュアル操作以外でスクリーンセーバーを起動しないように修正

* 0.0.0.2
  * スクリーンセーバー機能を追加
  * ドローン表示オフセット用のリサージュを追加
  * ドローン再生成時に前回の状態を保存するように修正
  * マニュアル操作にリセットキーを追加

* 0.0.0.1
  * アルゴリズム「ウェイポイント」「リサージュ」を追加
  * 設定ファイルのフォーマットを大幅修正
  * マニュアル操作に減速パラメータを追加
  * デバッグ出力キーを追加

@drorichcafe
