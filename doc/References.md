# 参考資料

実装にあたって参照した Web ページ・動画をまとめます。  
調査の流れが把握できるよう、検索キーワードとともに記録しています。

---

## 目次

1. [鏡・光学の基礎](#1-鏡光学の基礎)
2. [Unity 実装](#2-unity-実装)
3. [数学・ベクトル演算](#3-数学ベクトル演算)

---

## 1. 鏡・光学の基礎

### キヤノン「反射鏡ってなに？」

| 項目 | 内容 |
|------|------|
| 検索キーワード | 鏡　原理 |
| URL | https://global.canon/ja/technology/kids/mystery/m_02_05.html |

乱反射と鏡反射の違い、平面鏡・凸面鏡・凹面鏡それぞれの反射の特性と  
バックミラーや拡大鏡などへの応用例が解説されている。  
鏡面の種類（平面・球面・放物面）によって反射方向がどう変わるかを把握するために参照した。

---

### 京都教育大学「鏡に映る像」

| 項目 | 内容 |
|------|------|
| 検索キーワード | 鏡　仕組み　反射　視線（画像検索） |
| URL | https://natsci.kyokyo-u.ac.jp/~okihana/kaisetu/kagami.html |

1 枚・2 枚・複数の鏡の場合それぞれで像がどのように映るかを  
入射角と反射角の図解で段階的に説明している教育コンテンツ。  
「視点が変わると像の見え方がどう変わるか」の直感的な理解に活用した。

---

## 2. Unity 実装

### styly.cc「RenderTexture を使って鏡を作成する方法」

| 項目 | 内容 |
|------|------|
| 検索キーワード | unity ミラー 作り方 |
| URL | https://styly.cc/ja/tips/tomo-create-mirror-using-rendertexture/ |

RenderTexture にカメラ映像を書き込み、Quad マテリアルに適用する  
基本的な鏡の構築手順を解説。  
PlayMaker によるメインカメラ位置の追跡と反射カメラの動的配置まで踏み込んでいる。  
ベクトル演算方式（`MirrorCameraVector`）の実装方針を検討する際の出発点とした。

---

### Qiita — nkjzm「Unity で鏡を実装する方法」

| 項目 | 内容 |
|------|------|
| 検索キーワード | Unity ミラー 向き |
| URL | https://qiita.com/nkjzm/items/ccba41a6e7e5211aae95 |

反射カメラをミラー面と対称な位置に配置し、焦点距離・視野角を調整して  
スケール感を正確にする手法を紹介している。  
カメラ向きをミラー面の回転に連動させる実装例があり、  
`MirrorCameraVector` のカメラ位置・回転計算の参考にした。

---

### YouTube「Unity Mirror Reflection Script RenderTexture」

| 項目 | 内容 |
|------|------|
| 検索キーワード | unity mirror reflection script rendertexture |
| URL | https://www.youtube.com/watch?v=txF4t1qynyk |

RenderTexture を用いたリアルタイム反射の Unity スクリプト実装を  
動画で解説しているチュートリアル。  
コード全体の流れを把握するために参照した。

---

### Qiita — hikoalpha「【Unity】平面の鏡面反射を読み解く」

| 項目 | 内容 |
|------|------|
| 検索キーワード | unity 反射鏡 |
| URL | https://qiita.com/hikoalpha/items/8445109a20b8139ce7b5 |

Planar Reflection の仕組みを数学的に解説しており、  
反射カメラの配置計算・反射行列の導出・投影テクスチャマッピングによる  
シェーダー実装まで具体的なコードと数式で示されている。  
行列演算方式（`MirrorCamera` / `MirrorReflectionMatrix`）の実装にあたって最も参照した記事。

---

## 3. 数学・ベクトル演算

### araramistudio「DirectX11 で鏡面反射（Specular Reflection）」

| 項目 | 内容 |
|------|------|
| 検索キーワード | 鏡面反射 ベクトル計算 |
| URL | https://araramistudio.jimdofree.com/2017/10/02/プログラミング-directx-11で鏡面反射-specular-reflection/ |

DirectX 11 を題材に鏡面反射の計算式と実装を解説した記事。  
反射ベクトルの導出を GPU シェーダーの観点から説明しており、  
シェーダー側の UV 計算を検討する際の参考にした。

---

### Qiita — edo_m18「反射ベクトルを求める」

| 項目 | 内容 |
|------|------|
| 検索キーワード | 鏡面反射 ベクトル計算 |
| URL | https://qiita.com/edo_m18/items/b145f2f5d2d05f0f29c9 |

進行ベクトル **F** と法線ベクトル **N** の内積を使って  
反射ベクトル **R = F + 2aN**（a = −dot(F, N)）を導出する手順を解説している。  
`MirrorReflectionMatrix.cs` での数式実装の理解に活用した。

---

### nekojara.city「【Unity】Vector3.Reflect で反射ベクトルを求める」

| 項目 | 内容 |
|------|------|
| 検索キーワード | Unity 反射計算 |
| URL | https://nekojara.city/unity-vector-reflect |

Unity 組み込みの `Vector3.Reflect` の内部計算と使い方を解説している記事。  
`MirrorCameraVector` および `MirrorCameraVectorProjective` で  
`Vector3.Reflect` を用いた反射カメラ方向の計算を行うにあたり参照した。
