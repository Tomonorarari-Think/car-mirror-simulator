# システムアーキテクチャ

Unity 6 / URP で動作する車載ミラーシミュレーターの全体設計と、各レイヤー・クラスの責務分担を解説します。

---

## 目次

1. [ディレクトリ構成](#1-ディレクトリ構成)
2. [レイヤー設計の方針](#2-レイヤー設計の方針)
3. [名前空間と責務](#3-名前空間と責務)
4. [依存関係グラフ](#4-依存関係グラフ)
5. [操作切り替えの仕組み](#5-操作切り替えの仕組み)
6. [ミラーシステムの選択指針](#6-ミラーシステムの選択指針)

---

## 1. ディレクトリ構成

```
Assets/Scripts/
├── Core/           CarMirrorSimulator.Core      抽象・ユーティリティ
│   ├── IControllable.cs
│   ├── IMoveable.cs
│   ├── IRotatable.cs
│   └── Util.cs
│
├── Input/          CarMirrorSimulator.Input     入力抽象化
│   ├── AxisBinding.cs          (abstract)
│   ├── KeyboardAxisBinding.cs
│   └── MouseAxisBinding.cs
│
├── Mirror/         CarMirrorSimulator.Mirror    反射レンダリング
│   ├── MirrorCamera.cs                 行列演算方式
│   ├── MirrorCameraVector.cs           ベクトル演算方式
│   ├── MirrorCameraVectorProjective.cs ベクトル演算 + プロジェクティブUV
│   ├── MirrorReflectionMatrix.cs       反射行列ユーティリティ
│   ├── MirrorRenderFeature.cs          URP ScriptableRendererFeature
│   └── MirrorSurface.cs                反射面コンポーネント
│
├── Controller/     CarMirrorSimulator.Controller 操作ロジック
│   ├── MirrorController.cs             (abstract) ミラー回転基底
│   ├── SideMirrorController.cs         サイドミラー 2軸回転
│   ├── RoomMirrorController.cs         ルームミラー 3軸回転
│   ├── ReflectionTargetController.cs   映り込みオブジェクト移動
│   └── PlayerCameraController.cs       プレイヤーカメラ
│
└── Manager/        CarMirrorSimulator.Manager   シーン管理
    └── ControlSwitcher.cs

Assets/Shaders/
└── Mirror/
    ├── MirrorSurface.shader        行列方式対応シェーダー
    └── MirrorSurfaceVector.shader  ベクトル方式（プロジェクティブUV）対応
```

---

## 2. レイヤー設計の方針

このプロジェクトは **4 層構造** で構成されています。

```
┌─────────────────────────────────────────┐
│  Manager  （シーン管理・UI連携）          │
├─────────────────────────────────────────┤
│  Controller （操作ロジック）             │
├───────────────────┬─────────────────────┤
│  Input （入力抽象）│  Mirror （反射描画） │
├───────────────────┴─────────────────────┤
│  Core  （インターフェース・ユーティリティ）│
└─────────────────────────────────────────┘
```

### 設計方針

| 方針 | 理由 |
|------|------|
| **上位レイヤーは下位レイヤーにのみ依存する** | 変更の波及を最小化するため |
| **Mirror は Controller/Manager に依存しない** | 反射レンダリングはシーン固有ロジックと独立して再利用できるようにするため |
| **入力デバイスを Controller から切り離す** | 将来的にキーボード以外（ゲームパッド等）へ差し替えるコストを下げるため |
| **IControllable で操作対象を抽象化する** | Manager が各コントローラーの具体型を知らなくて済むようにするため |

---

## 3. 名前空間と責務

### 3.1 Core — 抽象・ユーティリティ

アプリケーション全体で使う **インターフェース定義** と **静的ユーティリティ** を置くレイヤー。
他の名前空間に依存せず、全レイヤーから参照可能。

| クラス | 種別 | 責務 |
|--------|------|------|
| `IControllable` | interface | 操作対象の有効化 / 無効化を統一するインターフェース。`ActivateControl()` / `DeactivateControl()` を定義する。Manager が具体型を知らずに切り替えを実現するための鍵となる |
| `IMoveable<T>` | interface | `Move(T direction)` を定義。2D・3D どちらの移動にも対応できるようジェネリクスを使用 |
| `IRotatable<T>` | interface | `Rotate(T eulerAngles)` を定義。`Vector2`（2軸）と `Vector3`（3軸）を型パラメーターで使い分ける |
| `Util` | static class | `NormalizeEulerAngle`（0〜360 → −180〜180 変換）と `AssertNotNull`（Null ガード、エディターでは一時停止）を提供する |

**`IControllable` / `IMoveable` / `IRotatable` を分離している理由**  
"操作できる" "動ける" "回転できる" は直交した概念。一つのコントローラーが複数の能力を持つこともあるため、単一のインターフェースにまとめると実装しなくていいメソッドが増える。分離することで、各コントローラーは必要な能力だけを宣言できる。

---

### 3.2 Input — 入力抽象化

**入力デバイスの種類をコントローラーロジックから分離** するレイヤー。
`AxisBinding` を継承したクラスを差し替えるだけで、キーボード / マウス / ゲームパッドを切り替えられる。

| クラス | 責務 |
|--------|------|
| `AxisBinding` (abstract) | `GetValue() → float` を定義する基底クラス。+1 / 0 / −1 の軸値を返す契約だけを持つ |
| `KeyboardAxisBinding` | 正側キー・負側キーの 2 キーを 1 軸に割り当てる。`[Serializable]` で Inspector 上で設定可能 |
| `MouseAxisBinding` | 旧 Input System の軸名（`"Mouse X"` 等）と感度倍率を持つ |

`[Serializable]` を付与することで、各コントローラーの Inspector から直接キー割り当てを設定できる。

---

### 3.3 Mirror — 反射レンダリング

ミラーの映像を生成する **レンダリング専用レイヤー**。
Controller / Manager には依存せず、シーンに独立して配置できる。

詳細な数学的背景は [MirrorReflection.md](./MirrorReflection.md) を参照。

| クラス | 責務 |
|--------|------|
| `MirrorCamera` | **行列演算方式**。`worldToCameraMatrix` に反射行列を乗算してミラーカメラのビューを構築し、RenderTexture を管理する。フレームスキップ・可視性カリング機能を持つ |
| `MirrorCameraVector` | **ベクトル演算方式**。`Vector3.Reflect` + `Quaternion.LookRotation` でカメラの位置・回転を直接設定する。シンプルで `GL.invertCulling` が不要 |
| `MirrorCameraVectorProjective` | `MirrorCameraVector` の上位互換。VP 行列を `MaterialPropertyBlock` でシェーダーに渡し、プロジェクティブ UV で正確な画像マッピングを実現する |
| `MirrorReflectionMatrix` | 反射行列の計算を担う純粋な静的ユーティリティ。`CalculateReflectionMatrix` / `WorldPlaneToVector4` / `GetCameraSpacePlane` を提供する |
| `MirrorRenderFeature` | URP ScriptableRendererFeature。行列方式が必要とする `GL.invertCulling` のON/OFFを、Render Graph パスとして Renderer Asset に組み込む |
| `MirrorSurface` | ミラーメッシュに付与するコンポーネント。位置・法線を提供し、`MaterialPropertyBlock` 経由で RenderTexture をマテリアルにバインドする |

**MirrorSurface を独立させている理由**  
カメラロジック（どう撮影するか）と表面描画（何を表示するか）を分離するため。
`SetRenderTexture(RenderTexture)` が公開 API になっているので、行列方式・ベクトル方式どちらのカメラからも RenderTexture を流し込める。

---

### 3.4 Controller — 操作ロジック

ユーザー入力を物体の移動・回転に変換する **ゲームプレイ層**。
`MirrorController` が Template Method パターンを採用し、サブクラスは入力取得と回転処理だけを実装する。

| クラス | 責務 |
|--------|------|
| `MirrorController` (abstract) | `IControllable` を実装し、`enabled` フラグで Update を制御する。サブクラスへ `OnUpdateRotation()` / `TryGetRotateInput()` の実装を要求する |
| `SideMirrorController` | `MirrorController` を継承。X・Y 2軸のキーボード入力で回転原点を回転させる。`IRotatable<Vector2>` を実装 |
| `RoomMirrorController` | `MirrorController` を継承。X・Y・Z 3軸のキーボード入力で回転させる。`IRotatable<Vector3>` を実装 |
| `ReflectionTargetController` | 鏡に映るオブジェクトを 3軸キーボードで移動させる。`IControllable` + `IMoveable<Vector3>` を実装。起動時に初期座標を記録して `PositionReset()` で戻せる |
| `PlayerCameraController` | キーボード移動 + 右クリックドラッグによるマウス回転でプレイヤーカメラを操作する |

**Template Method パターンを採用している理由**  
`Update → TryGetRotateInput → OnUpdateRotation` というフレームあたりの処理フローは全ミラーコントローラーで共通。フローを基底クラスに固定し、サブクラスは差異（軸数・入力キー）だけを実装すれば良い構造にした。

---

### 3.5 Manager — シーン管理

UI やシーン全体の調停を担う **最上位レイヤー**。

| クラス | 責務 |
|--------|------|
| `ControlSwitcher` | Inspector で `(Button, Transform)` のペアリストを設定する。各ボタン押下時に対応する `IControllable` を有効化し、他を無効化する。具体的なコントローラー型を一切知らずに排他切り替えを実現する |

**`IControllable` に依存している理由**  
ボタン数が変わっても `entries` リストを増減するだけでよく、`ControlSwitcher` 自体のコードを変更しなくて良い。新しい操作対象が増えても `IControllable` を実装するだけで自動的に切り替え対象に追加できる。

---

## 4. 依存関係グラフ

```
Manager
  └── Core (IControllable)
       ↑
Controller ──── Input (AxisBinding系)
  └── Core (IControllable, IMoveable, IRotatable, Util)

Mirror (独立)
  └── (自己完結。Core/Input/Controller/Manager に依存しない)
```

Unity の `MonoBehaviour` および `UnityEngine.*` への依存はどの層にも存在するが、
それ以外の「アプリ内クロス依存」は上記の単方向グラフに限定している。

---

## 5. 操作切り替えの仕組み

`ControlSwitcher` が実現する排他切り替えのシーケンスを示します。

```
[ボタン A を押す]
    │
    ▼
ControlSwitcher.RegisterListeners で登録されたコールバック群が発火
    │
    ├── controls[A].ActivateControl()    → 対応コントローラーの enabled = true
    ├── controls[B].DeactivateControl()  → 他コントローラーの enabled = false
    └── controls[C].DeactivateControl()  → ...
```

コントローラーは `enabled = false` のとき `Update()` が呼ばれないため、
入力受付・物体操作が自動的に停止する。
`ControlSwitcher` は `IControllable` のみを知っており、実際のコントローラー型（`SideMirrorController` など）を知らない。

---

## 6. ミラーシステムの選択指針

| | 行列演算方式 | ベクトル演算方式 | ベクトル + プロジェクティブ |
|--|------------|--------------|--------------------------|
| **クラス** | `MirrorCamera` | `MirrorCameraVector` | `MirrorCameraVectorProjective` |
| **シェーダー** | `MirrorSurface` | `MirrorSurface` または任意 | `MirrorSurfaceVector` |
| **URP 設定** | `MirrorRenderFeature` が必要 | 不要 | 不要 |
| **精度** | 高（オブリーク投影でクリッピング正確） | 中（ニアクリップは固定） | 高（プロジェクティブ UV で歪み補正） |
| **セットアップ難度** | やや複雑 | シンプル | 中程度 |
| **ユースケース** | 高品質サイドミラー・ルームミラー | プロトタイプ・軽量シーン | UV 精度が重要な平面ミラー |

各方式の数学的詳細は [MirrorReflection.md](./MirrorReflection.md) を参照してください。
