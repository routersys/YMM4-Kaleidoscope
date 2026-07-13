# v1.0.0 - 万華鏡 for YMM4

YukkuriMovieMaker4 向けの万華鏡エフェクトプラグインの初回リリースです。
Direct2D カスタムピクセルシェーダーが、中心からの角度を分割数で割った扇形へ各画素を折り込み、入力映像をサンプリングして対称模様を作ります。
ミラーリングで隣り合う扇形を鏡合わせにするか回転コピーにするかを切り替え、回転・ズーム・中心位置で模様を動かし、適用量で元映像との割合を調整します。
8 言語リソース構成の UI を備えます。

---

## 新機能

### 1. ピクセルシェーダー

`Kaleidoscope.hlsl` の `main` は、中心からの角度を扇形へ折り込み、その方向で入力映像をサンプリングします。追加テクスチャは使用しません。`amount <= 0` のときはソースをそのまま返します。

#### 中心と角度の折り込み

入力矩形 `inputBounds` から中心 `imageCenter` を求め、`centerX`・`centerY` を映像の半分の大きさ `halfSize` に掛けてずらした位置を万華鏡の中心とします。中心からの差分 `delta` の角度を `atan2` で求め、`rotation` を引いてから扇形の角度 `segmentAngle = 2π / max(segments, 1)` で折り込みます。

| 値 | 説明 |
|---|---|
| `center` | `imageCenter + (centerX, centerY) × halfSize` |
| `segmentAngle` | `2π / max(segments, 1)` の 1 扇形の角度 |
| `folded` | 扇形へ折り込んだ角度 |

#### ミラーリング

`mirror >= 0.5` のとき、折り込んだ角度を扇形の中央 `halfSegment` で反射させ、隣り合う扇形を鏡合わせにします。無効のときは折り込みのみで回転コピーになります。折り込んだ角度に `rotation` を戻して参照方向を求めます。

#### サンプリングと合成

半径を `zoom` で割った `sampledRadius` と参照方向から参照位置を求め、`inputBounds` の内側 0.5 へクランプしてから `uv0.zw` で UV へ変換してサンプリングします。最後に `lerp(source, kaleido, amount)` で元映像と混ぜます。

| 項目 | 式 |
|---|---|
| 参照半径 | `radius / max(zoom, 1e-3)` |
| 参照位置 | `center + float2(cos(folded), sin(folded)) × sampledRadius` |
| 出力色 | `lerp(source, kaleido, amount)` |

---

### 2. カスタムシェーダーエフェクト

`KaleidoscopeCustomEffect` は `[CustomEffect(1)]` の 1 入力エフェクトです。公開プロパティは `SetValue` を介して定数バッファーへ転送します。各プロパティは代入時にシェーダーが前提とする範囲へ制限します。

| プロパティ | 型 | 範囲 |
|---|---|---|
| `Segments` | `float` | 1〜256 |
| `Rotation` | `float` | ラジアン |
| `Zoom` | `float` | 1e-3〜1e4 |
| `CenterX` | `float` | 制限なし |
| `CenterY` | `float` | 制限なし |
| `Mirror` | `float` | 0〜1 |
| `Amount` | `float` | 0〜1 |

`ConstantBuffer` のレイアウトは以下のとおりです。末尾に詰め物を置き、合計 48 バイトを 16 バイトの倍数に揃えます。

| フィールド | 型 | 説明 |
|---|---|---|
| `InputBounds` | `float4` | 入力矩形の左上・右下 |
| `Segments` | `float` | 分割数 |
| `Rotation` | `float` | 回転（ラジアン） |
| `Zoom` | `float` | ズーム |
| `CenterX` | `float` | 中心 X |
| `CenterY` | `float` | 中心 Y |
| `Mirror` | `float` | ミラーリング |
| `Amount` | `float` | 適用量 |
| `Pad0` | `float` | 詰め物 |

`MapInputRectsToOutputRect` は基底の対応付けで出力矩形を求めた後、クランプした入力矩形を `InputBounds` へ書き込みます。`MapOutputRectToInputRects` は入力矩形をそのまま返します。出力画素が入力映像の任意の位置を参照するため、入力矩形の拡張は行いません。

シェーダーリソース: `pack://application:,,,/Kaleidoscope;component/Shaders/Kaleidoscope.cso`（ps_5_0、`ShaderResourceUri.Get` が生成）

---

### 3. エフェクト定義

`KaleidoscopeEffect` は YMM4 の映像エフェクトとして宣言されます。

`[VideoEffect]` 属性は以下のパラメーターで宣言されます。

- 表示名：`Texts.Kaleidoscope`（ローカライズキー、日本語では「万華鏡」）
- カテゴリー：`VideoEffectCategories.Filtering`
- 検索タグ：`TagKaleidoscope`・`TagMirror`・`TagSymmetry`
- `IsAviUtlSupported = false` により AviUtl 向け EXO 出力は非対応
- `ResourceType = typeof(Texts)` でローカライズリソースを指定

`Label` プロパティは `Texts.Kaleidoscope` を返します。

公開プロパティは以下のとおりです。

| プロパティ | 型 | デフォルト | 内部範囲 | アニメーション |
|---|---|---|---|---|
| `Segments` | `Animation` | 6 | 1〜256 | あり |
| `Rotation` | `Animation` | 0 | -36000〜36000 | あり |
| `Zoom` | `Animation` | 100 | 1〜2000 | あり |
| `CenterX` | `Animation` | 0 | -500〜500 | あり |
| `CenterY` | `Animation` | 0 | -500〜500 | あり |
| `Amount` | `Animation` | 100 | 0〜100 | あり |
| `Mirror` | `bool` | true | — | なし |

`GetAnimatables` は `Segments`・`Rotation`・`Zoom`・`CenterX`・`CenterY`・`Amount` を返します。

`CreateExoVideoFilters` は空のシーケンスを返します（EXO 非対応）。`CreateVideoEffect` は映像処理用のインスタンスを生成します。

---

### 4. フレームごとの更新

各フレームで YMM4 の `EffectDescription` からフレーム位置、アイテム長、FPS を取得し、アニメーション値を評価します。前フレームと値が異なる項目だけをカスタムシェーダーへ転送します。

| パラメータ | 変換 |
|---|---|
| `Segments` | 数値のまま |
| `Rotation` | 度からラジアンへ変換 |
| `Zoom` | `value / 100` |
| `CenterX` | `value / 100` |
| `CenterY` | `value / 100` |
| `Mirror` | 真偽値を 1 または 0 へ |
| `Amount` | `value / 100` |

入力は `SetInput(0, input, true)` でカスタムシェーダーへ接続します。エフェクトチェーンのクリア時は入力 0 を `null` に戻します。

---

### 5. ローカライズ

`Texts` クラスは `[AutoGenLocalizer]` 属性を持つ `partial` クラスとして宣言されます。
`YukkuriMovieMaker.Generator` のソースジェネレーターが `Texts.csv` を処理し、各ロケールのリソースファイルを自動生成します。

対応リソース：日本語（`ja-jp`）・英語（`en-us`）・中国語簡体字（`zh-cn`）・中国語繁体字（`zh-tw`）・韓国語（`ko-kr`）・スペイン語（`es-es`）・アラビア語（`ar-sa`）・インドネシア語（`id-id`）

ローカライズキーの一覧は以下のとおりです。

| キー | ja-jp |
|---|---|
| `Kaleidoscope` | 万華鏡 |
| `TagKaleidoscope` | 万華鏡 |
| `TagMirror` | 鏡 |
| `TagSymmetry` | 対称 |
| `Segments` | 分割数 |
| `Rotation` | 回転 |
| `Zoom` | ズーム |
| `CenterX` | 中心X |
| `CenterY` | 中心Y |
| `Amount` | 適用量 |
| `Mirror` | ミラーリング |
