# クラスリファレンス

各クラスの公開 API・Inspector 設定・使用上の注意をまとめたリファレンスです。  
設計方針や全体像は [Architecture.md](./Architecture.md) を参照してください。

---

## 目次

- [Core](#core)
- [Input](#input)
- [Mirror](#mirror)
- [Controller](#controller)
- [Manager](#manager)

---

## Core

### `IControllable`

```csharp
namespace CarMirrorSimulator.Core

public interface IControllable
{
    void ActivateControl();
    void DeactivateControl();
}
```

操作対象を統一するインターフェース。`ControlSwitcher` はこの型のみを通じてコントローラーを切り替える。

---

### `IMoveable<T>`

```csharp
public interface IMoveable<T>
{
    void Move(T direction);
}
```

移動できる物体の契約。`T` には `Vector2` / `Vector3` などを使用する。

---

### `IRotatable<T>`

```csharp
public interface IRotatable<T>
{
    void Rotate(T eulerAngles);
}
```

回転できる物体の契約。`T` には `Vector2`（2軸）または `Vector3`（3軸）を指定する。

---

### `Util`

```csharp
public static class Util
```

| メソッド | 説明 |
|----------|------|
| `NormalizeEulerAngle(float) → float` | Unity の `eulerAngles`（0〜360）を −180〜180 に変換する。Clamp と組み合わせて角度制限を正しく機能させるために使用する |
| `AssertNotNull(object, string)` | `null` なら `Debug.LogError` を出力しエディターを一時停止、ビルドでは `Application.Quit()` |

---

## Input

### `AxisBinding` (abstract)

```csharp
namespace CarMirrorSimulator.Input

public abstract class AxisBinding
{
    public abstract float GetValue();  // 戻り値: −1 / 0 / +1 （またはそれに準ずる値）
}
```

全入力バインディングの基底。コントローラーは `AxisBinding` の配列を持つことで、入力デバイスを切り替えられる。

---

### `KeyboardAxisBinding : AxisBinding`

```csharp
[Serializable]
public class KeyboardAxisBinding : AxisBinding
```

| フィールド | 説明 |
|-----------|------|
| `positive (KeyCode)` | +1 を返すキー |
| `negative (KeyCode)` | −1 を返すキー |

`[Serializable]` のため `[SerializeField]` と組み合わせて Inspector で設定できる。

---

### `MouseAxisBinding : AxisBinding`

```csharp
[Serializable]
public class MouseAxisBinding : AxisBinding
```

| フィールド | 説明 |
|-----------|------|
| `axisName (string)` | 旧 Input System の軸名。`"Mouse X"` / `"Mouse Y"` など |
| `sensitivity (float)` | 取得した値に掛ける倍率 |

---

## Mirror

### `MirrorCamera`

```csharp
[RequireComponent(typeof(Camera))]
[AddComponentMenu("CarMirror/Mirror Camera")]
public class MirrorCamera : MonoBehaviour
```

**行列演算方式のミラーカメラ**。ビュー行列に反射行列を乗算してミラー映像を生成する。

#### Inspector 設定

| フィールド | デフォルト | 説明 |
|-----------|------------|------|
| `_mainCamera` | Camera.main | 反射計算の基準カメラ |
| `_mirrorSurface` | — | 平面の位置・法線を提供する `MirrorSurface` |
| `_textureWidth` | 512 | RenderTexture の横解像度（px） |
| `_resolutionScale` | 1.0 | 解像度スケール (0.25〜1.0)。下げると GPU 負荷が下がる |
| `_frameSkip` | 1 | N フレームに 1 回だけ描画する（1 = 毎フレーム） |

#### 公開プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `RenderTexture` | `RenderTexture` | ミラー映像が描画される RenderTexture。`MirrorSurface` から参照される |

#### セットアップ

1. ミラーカメラ GameObject に `Camera` と `MirrorCamera` を追加する
2. `_mainCamera` / `_mirrorSurface` を Inspector で設定する
3. カメラの `depth` をメインカメラより小さい値（例: −10）にする
4. URP Renderer Asset に `MirrorRenderFeature` を追加する

#### 静的メンバー

`ActiveMirrorCameras (HashSet<Camera>)` — アクティブな全 MirrorCamera の Camera を格納する。`MirrorRenderFeature` がこれを参照して `GL.invertCulling` の適用対象を O(1) で判定する。

---

### `MirrorCameraVector`

```csharp
[AddComponentMenu("CarMirror/Mirror Camera (Vector)")]
public class MirrorCameraVector : MonoBehaviour
```

**ベクトル演算方式のミラーカメラ**。`Vector3.Reflect` + `Quaternion.LookRotation` でカメラ位置・回転を設定する。
`GL.invertCulling` が不要で、`MirrorRenderFeature` を Renderer Asset に追加する必要がない。

#### Inspector 設定

| フィールド | デフォルト | 説明 |
|-----------|------------|------|
| `_mainCamera` | Camera.main | 基準カメラ |
| `_mirrorCamera` | — | 制御するミラーカメラ（Target Texture に RenderTexture を設定済みであること） |
| `_mirrorTransform` | — | ミラー面の Transform |
| `_localNormal` | (0,0,1) | ローカル空間でのミラー法線 |
| `_cameraOffset` | 0.01 | ミラー面からカメラを手前に置くオフセット（m）。ニアクリップ面の交差を防ぐ |
| `_gimbalLockThreshold` | 0.99 | ジンバルロック検出閾値。反射ベクトルと up の内積がこの値を超えたら right にフォールバック |

---

### `MirrorCameraVectorProjective`

```csharp
[AddComponentMenu("CarMirror/Mirror Camera (Vector Projective)")]
public class MirrorCameraVectorProjective : MonoBehaviour
```

`MirrorCameraVector` の上位互換。`MirrorSurfaceVector` シェーダー用に VP 行列と RenderTexture を `MaterialPropertyBlock` で毎フレーム送信する。

#### MirrorCameraVector との差分

| 追加フィールド | 説明 |
|--------------|------|
| `_mirrorRenderer (Renderer)` | `MirrorSurfaceVector` マテリアルを持つミラーメッシュの Renderer |

#### シェーダーへの送信プロパティ

| シェーダープロパティ | 内容 |
|--------------------|----|
| `_MirrorVP` | `GL.GetGPUProjectionMatrix(proj, true) × worldToCamera` の行列。GPU の Y 反転を吸収済み |
| `_MirrorTex` | ミラーカメラの `targetTexture` |

---

### `MirrorReflectionMatrix`

```csharp
public static class MirrorReflectionMatrix
```

反射行列計算の純粋な数学ユーティリティ。`MirrorCamera` から呼ばれる。

| メソッド | 説明 |
|----------|------|
| `CalculateReflectionMatrix(Vector4 plane) → Matrix4x4` | `M = I - 2 * N * Nᵀ` の 4×4 同次座標行列を返す |
| `WorldPlaneToVector4(Vector3 pos, Vector3 normal) → Vector4` | 点と法線から平面方程式係数 `(a, b, c, d)` を返す。`d = -dot(N, P)` |
| `GetCameraSpacePlane(Camera, Vector3, Vector3, float) → Vector4` | ワールド空間の平面をカメラ空間に変換して返す。`Camera.CalculateObliqueMatrix` への入力として使用する |

---

### `MirrorRenderFeature`

```csharp
public class MirrorRenderFeature : ScriptableRendererFeature
```

**行列演算方式専用の URP 拡張**。反射行列はハンドネスを反転させるため、ミラーカメラの描画前後で `GL.invertCulling` を切り替えるパスをキューに追加する。

- `MirrorCamera.ActiveMirrorCameras` を参照し、ミラーカメラ以外のカメラには適用しない
- URP Render Graph モードの `AddUnsafePass` と、互換モード用の `Execute` 両方を実装している

#### セットアップ

Project Settings > Graphics > URP Renderer Asset の Add Renderer Feature から追加する。

---

### `MirrorSurface`

```csharp
[RequireComponent(typeof(Renderer))]
[AddComponentMenu("CarMirror/Mirror Surface")]
public class MirrorSurface : MonoBehaviour
```

ミラーメッシュに付与するコンポーネント。カメラ計算の基準となる位置・法線を提供し、RenderTexture をマテリアルにバインドする。

#### Inspector 設定

| フィールド | デフォルト | 説明 |
|-----------|------------|------|
| `_mirrorCamera (MirrorCamera)` | — | 対応する行列方式カメラ（ベクトル方式のみ使う場合は不要） |
| `_localNormal` | (0,0,1) | ミラー面の法線（ローカル空間）。Quad は (0,0,1)、Plane は (0,1,0) |
| `_texturePropertyName` | `"_MirrorTex"` | マテリアルの RenderTexture プロパティ名 |

#### プロパティ

| プロパティ | 説明 |
|-----------|------|
| `MirrorPosition` | `transform.position`（ワールド空間） |
| `MirrorNormal` | `_localNormal` をワールド空間へ変換した正規化ベクトル |
| `IsVisible` | `Renderer.isVisible`。MirrorCamera がフレームスキップ判定に使用する |

#### 公開メソッド

| メソッド | 説明 |
|---------|------|
| `RefreshRenderTexture()` | `_mirrorCamera.RenderTexture` をマテリアルに再バインドする。解像度変更後など |
| `SetRenderTexture(RenderTexture)` | 任意の RenderTexture をバインドする。ベクトル方式カメラからも呼び出せる |

---

## Controller

### `MirrorController` (abstract)

```csharp
public abstract class MirrorController : MonoBehaviour, IControllable
```

ミラー回転コントローラーの基底クラス。Template Method パターンで Update フローを固定する。

- `Awake` で `_rotateOrigin` の Null チェックを行い、`enabled = false` で待機状態にする
- `ActivateControl()` / `DeactivateControl()` は `enabled` を切り替えるだけ

#### サブクラスが実装するメソッド

| メソッド | 説明 |
|---------|------|
| `OnUpdateRotation()` | 毎フレーム呼ばれる。入力から回転量を算出して `_rotateOrigin` を回転させる |
| `TryGetRotateInput() → bool` | 入力値を取得して変数に格納し、入力があれば `true` を返す |

#### Inspector 設定

| フィールド | 説明 |
|-----------|------|
| `_rotateOrigin (Transform)` | 回転の原点。ミラー本体の親 Transform を設定する |

---

### `SideMirrorController : MirrorController, IRotatable<Vector2>`

X（上下）・Y（左右）2 軸でミラーを回転させる。デフォルトキーは W/S（X軸）、D/A（Y軸）。

| Inspector フィールド | デフォルト | 説明 |
|--------------------|------------|------|
| `_xAxis / _yAxis` | W/S, D/A | キーバインド |
| `_rotMinEulerAngles / _rotMaxEulerAngles` | ±180° | 各軸の回転制限 |
| `_rotateAngles` | (1,1) | 1 秒あたりの回転角度（度） |

---

### `RoomMirrorController : MirrorController, IRotatable<Vector3>`

X・Y・Z 3 軸でミラーを回転させる。デフォルトキーは W/S（X）、D/A（Y）、E/Q（Z）。

| Inspector フィールド | デフォルト | 説明 |
|--------------------|------------|------|
| `_xAxis / _yAxis / _zAxis` | W/S, D/A, E/Q | キーバインド |
| `_rotMinEulerAngles / _rotMaxEulerAngles` | ±180° | 各軸の回転制限 |
| `_rotateAngles` | (1,1,1) | 1 秒あたりの回転角度（度） |

---

### `ReflectionTargetController : MonoBehaviour, IControllable, IMoveable<Vector3>`

鏡に映るオブジェクトを 3 軸キーボードで移動させるコントローラー。

| Inspector フィールド | デフォルト | 説明 |
|--------------------|------------|------|
| `_target (Transform)` | — | 移動させるオブジェクト |
| `_xDir / _yDir / _zDir` | D/A, E/Q, W/S | 各軸のキーバインド |
| `_moveSpeed` | 1.0 | 移動速度（m/s） |

`PositionReset()` で起動時の初期座標に戻せる。

---

### `PlayerCameraController : MonoBehaviour, IControllable, IMoveable<Vector3>, IRotatable<Vector3>`

キーボード移動（WASD / EQ）＋ 右クリックドラッグによるマウス視点回転でプレイヤーカメラを操作する。

| Inspector フィールド | デフォルト | 説明 |
|--------------------|------------|------|
| `_xDir / _yDir / _zDir` | D/A, E/Q, W/S | 移動キーバインド |
| `_xAxis / _yAxis` | Mouse Y, Mouse X | 視点回転（マウス） |
| `_moveSpeed` | 1.0 | 移動速度（m/s） |
| `_rotateAngles` | (1,1) | 1 秒あたりの回転角度（度） |

`ActivateControl()` / `DeactivateControl()` は `_isMove` フラグを切り替える（移動の有効化のみ制御。回転は右クリック中は常に有効）。

---

## Manager

### `ControlSwitcher`

```csharp
public class ControlSwitcher : MonoBehaviour
```

UI ボタンによる操作対象の排他切り替えを管理する。

#### Inspector 設定

`entries (List<ControlEntry>)` — ボタンと操作対象のペアリスト。

```
ControlEntry
  ├── ActivateButton (Button)  このボタンを押すと対応コントローラーが有効化される
  └── Target (Transform)       IControllable を持つ GameObject
```

#### 動作

- `Awake` で `Target.GetComponent<IControllable>()` を解決してリストに保存する
- `Start` で各ボタンの `onClick` にリスナーを登録する
- ボタン押下時: `controls[i].ActivateControl()` + 他の全 `controls[j].DeactivateControl()`

`Target` が `IControllable` を実装していない場合、`controls[i]` が `null` になり `NullReferenceException` が発生するため、設定時に注意する。
