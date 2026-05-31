# CarMirrorSimulator

Unity 6 / URP 対応の車載ミラー反射シミュレーター。  
バックミラー・サイドミラーに使用できる平面反射鏡を、3 種類の実装方式で提供します。

---

## 機能

| 機能 | 説明 |
|------|------|
| **行列演算方式** | ビュー行列に反射行列を乗算する高精度な実装 |
| **ベクトル演算方式** | `Vector3.Reflect` + `LookRotation` によるシンプルな実装 |
| **プロジェクティブ UV 方式** | ベクトル演算 + VP 行列によるスクリーン空間 UV の精密マッピング |
| オブリーク投影 | ミラー平面を near クリップとして設定し、背面への映り込みを防止 |
| フレームスキップ | 指定フレームに 1 回だけ描画してパフォーマンスを調整 |
| 可視性カリング | ミラーメッシュが画面外のときは描画をスキップ |
| 解像度スケール | RenderTexture の解像度を係数で下げて負荷を調整 |
| ミラー角度調整 | キーボード入力でルームミラー・サイドミラーの向きをリアルタイム調整 |
| 操作対象切り替え | UI ボタンでカメラ・ミラー・オブジェクトの操作を排他切り替え |

---

## 動作環境

| 項目 | バージョン |
|------|-----------|
| Unity | 6.x |
| Render Pipeline | Universal Render Pipeline (URP) |
| スクリプティング | C# (.NET Standard 2.1) |

---

## ファイル構成

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── IControllable.cs            # 操作対象インターフェース
│   │   ├── IMoveable.cs                # 移動インターフェース
│   │   ├── IRotatable.cs               # 回転インターフェース
│   │   └── Util.cs                     # 汎用ユーティリティ
│   ├── Input/
│   │   ├── AxisBinding.cs              # 入力抽象基底クラス
│   │   ├── KeyboardAxisBinding.cs      # キーボード軸入力
│   │   └── MouseAxisBinding.cs         # マウス軸入力
│   ├── Mirror/
│   │   ├── MirrorReflectionMatrix.cs   # 反射行列・平面方程式の数学ユーティリティ
│   │   ├── MirrorCamera.cs             # 行列演算方式カメラ制御
│   │   ├── MirrorSurface.cs            # ミラー面コンポーネント（法線・RenderTexture バインド）
│   │   ├── MirrorRenderFeature.cs      # URP Renderer Feature（GL.invertCulling 補正）
│   │   ├── MirrorCameraVector.cs       # ベクトル演算方式カメラ制御
│   │   └── MirrorCameraVectorProjective.cs  # プロジェクティブ UV 対応版ベクトルカメラ
│   ├── Controller/
│   │   ├── MirrorController.cs         # ミラー回転コントローラー抽象基底
│   │   ├── RoomMirrorController.cs     # ルームミラー（3 軸）回転コントローラー
│   │   ├── SideMirrorController.cs     # サイドミラー（2 軸）回転コントローラー
│   │   ├── PlayerCameraController.cs   # プレイヤーカメラ移動・回転コントローラー
│   │   └── ReflectionTargetController.cs  # 映り込みオブジェクト移動コントローラー
│   └── Manager/
│       └── ControlSwitcher.cs          # UI ボタンによる操作対象の排他切り替え
├── Shaders/Mirror/
│   ├── MirrorSurface.shader            # 行列方式用シェーダー（スクリーン空間 UV）
│   └── MirrorSurfaceVector.shader      # プロジェクティブ UV 方式用シェーダー
├── Materials/
│   ├── CarMirror_MirrorSurface.mat     # ルームミラー用マテリアル
│   ├── CarMirror_MirrorSurface_SideL.mat  # 左サイドミラー用マテリアル
│   └── CarMirror_MirrorSurface_SideR.mat  # 右サイドミラー用マテリアル
└── Scenes/
    └── Main.unity                      # メインシーン

doc/
├── MirrorReflection.md     # 実装ロジック・数学的解説
├── Architecture.md         # システムアーキテクチャ・レイヤー設計
├── ClassReference.md       # クラスリファレンス（公開 API・Inspector 設定）
└── image/                  # 解説用 SVG 図
```

---

## セットアップ

### 共通

1. `Assets/Shaders/Mirror/` のシェーダーを使ったマテリアルを作成する
2. シーンにミラー面となる Quad を配置し、そのマテリアルを設定する

### 行列演算方式（`MirrorCamera`）

```
[ミラーカメラ GameObject]
  ├─ Camera（depth: -10、targetTexture: なし ※スクリプトが自動生成）
  ├─ MirrorCamera（MainCamera・MirrorSurface を設定）
  └─ （MirrorRenderFeature を PC_Renderer.asset に追加）

[Quad GameObject]
  ├─ Renderer（MirrorSurface シェーダーのマテリアルを設定）
  └─ MirrorSurface（MirrorCamera・LocalNormal を設定）
```

> **URP Renderer Feature の追加**  
> `Assets/Settings/PC_Renderer.asset` を開き、**Add Renderer Feature → Mirror Render Feature** を追加する。

### ベクトル演算方式（`MirrorCameraVector`）

```
[ミラーカメラ GameObject]
  └─ Camera（Target Texture に RenderTexture を設定）

[任意の GameObject]
  └─ MirrorCameraVector（MainCamera・MirrorCamera・MirrorTransform を設定）

[Quad GameObject]
  └─ Renderer（RenderTexture を参照するマテリアルを設定）
```

> Renderer Feature の追加は**不要**です。

### プロジェクティブ UV 方式（`MirrorCameraVectorProjective`）

```
[ミラーカメラ GameObject]
  └─ Camera（Target Texture に RenderTexture を設定）

[任意の GameObject]
  └─ MirrorCameraVectorProjective（MainCamera・MirrorCamera・MirrorTransform・MirrorRenderer を設定）

[Quad GameObject]
  └─ Renderer（MirrorSurfaceVector シェーダーのマテリアルを設定）
```

> `MirrorSurfaceVector.shader` を使用します。VP 行列と RenderTexture はスクリプトが `MaterialPropertyBlock` で毎フレーム自動設定します。

---

## Inspector パラメーター

### MirrorCamera（行列方式）

| パラメーター | 説明 | デフォルト |
|------------|------|-----------|
| Main Camera | メインカメラ | Camera.main |
| Mirror Surface | 対応する MirrorSurface | — |
| Texture Width | RenderTexture 横解像度 (px) | 512 |
| Resolution Scale | 解像度スケール係数 | 1.0 |
| Frame Skip | N フレームに 1 回描画 | 1 |

### MirrorSurface

| パラメーター | 説明 | デフォルト |
|------------|------|-----------|
| Mirror Camera | 対応する MirrorCamera | — |
| Local Normal | 法線方向（ローカル空間） | (0,0,1) ← Quad |
| Texture Property Name | シェーダープロパティ名 | `_MirrorTex` |

> Plane を使う場合は Local Normal を **(0, 1, 0)** に変更する。

### MirrorCameraVector（ベクトル方式）

| パラメーター | 説明 | デフォルト |
|------------|------|-----------|
| Main Camera | メインカメラ | Camera.main |
| Mirror Camera | 制御するミラーカメラ | — |
| Mirror Transform | ミラー面の Transform | — |
| Local Normal | 法線方向（ローカル空間） | (0,0,1) |
| Camera Offset | ミラー面からのオフセット距離 (m) | 0.01 |
| Gimbal Lock Threshold | ジンバルロック検出閾値 | 0.99 |

### MirrorCameraVectorProjective（プロジェクティブ UV 方式）

| パラメーター | 説明 | デフォルト |
|------------|------|-----------|
| Main Camera | メインカメラ | Camera.main |
| Mirror Camera | 制御するミラーカメラ | — |
| Mirror Transform | ミラー面の Transform | — |
| Mirror Renderer | VP 行列・テクスチャを設定する Renderer | — |
| Local Normal | 法線方向（ローカル空間） | (0,0,1) |
| Camera Offset | ミラー面からのオフセット距離 (m) | 0.01 |
| Gimbal Lock Threshold | ジンバルロック検出閾値 | 0.99 |

### RoomMirrorController / SideMirrorController

| パラメーター | 説明 |
|------------|------|
| Rotate Origin | 回転の原点 Transform |
| X / Y / Z Axis | キーボード軸入力の設定 |
| Rot Min/Max Euler Angles | 角度制限（最小・最大） |
| Rotate Angles | 1 秒あたりの回転角度 |

---

## 操作方法（メインシーン）

UI ボタンで操作対象を切り替えます。

| 操作対象 | キー | 動作 |
|---------|------|------|
| プレイヤーカメラ | W / A / S / D / Q / E | 移動 |
| プレイヤーカメラ | 右クリック + マウス | 視点回転 |
| ルームミラー / サイドミラー | W / A / S / D / Q / E | ミラー角度調整 |
| 映り込みオブジェクト | W / A / S / D / Q / E | オブジェクト移動 |

---

## ドキュメント

| ファイル | 内容 |
|---------|------|
| [doc/MirrorReflection.md](doc/MirrorReflection.md) | 反射行列・オブリーク投影・UV マッピングの数学的解説 |
| [doc/Architecture.md](doc/Architecture.md) | システムアーキテクチャ・レイヤー設計・依存関係グラフ |
| [doc/ClassReference.md](doc/ClassReference.md) | 各クラスの公開 API・Inspector 設定・使用上の注意 |

---

## ライセンス

MIT License
