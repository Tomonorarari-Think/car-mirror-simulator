# CarMirrorSimulator

Unity 6 / URP 対応の車載ミラー反射シミュレーター。  
バックミラー・サイドミラーに使用できる平面反射鏡を、2 種類の実装方式で提供します。

---

## 機能

| 機能 | 説明 |
|------|------|
| **行列演算方式** | ビュー行列に反射行列を乗算する高精度な実装 |
| **ベクトル演算方式** | `Vector3.Reflect` + `LookRotation` によるシンプルな実装 |
| オブリーク投影 | ミラー平面を near クリップとして設定し、背面への映り込みを防止 |
| フレームスキップ | 指定フレームに 1 回だけ描画してパフォーマンスを調整 |
| 可視性カリング | ミラーメッシュが画面外のときは描画をスキップ |
| 解像度スケール | RenderTexture の解像度を係数で下げて負荷を調整 |

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
├── Scripts/Mirror/
│   ├── MirrorReflectionMatrix.cs   # 反射行列・平面方程式の数学ユーティリティ
│   ├── MirrorCamera.cs             # 行列演算方式カメラ制御
│   ├── MirrorSurface.cs            # ミラー面コンポーネント（法線・RenderTexture バインド）
│   ├── MirrorRenderFeature.cs      # URP Renderer Feature（GL.invertCulling 補正）
│   └── MirrorCameraVector.cs       # ベクトル演算方式カメラ制御
└── Shaders/Mirror/
    └── MirrorSurface.shader        # URP ミラー面シェーダー（スクリーン空間 UV）

doc/
└── MirrorReflection.md             # 実装ロジック・数学的解説
```

---

## セットアップ

### 共通

1. `Assets/Shaders/Mirror/MirrorSurface.shader` を使ったマテリアルを作成する
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

---

## 実装解説

詳細な数学的解説・図解は [doc/MirrorReflection.md](doc/MirrorReflection.md) を参照してください。

---

## ライセンス

MIT License
