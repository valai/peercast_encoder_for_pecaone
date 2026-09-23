# スマホ連携 API v1

Windows アプリは Tailscale の IPv4 アドレスだけで HTTP を待ち受けます。Tailscale のトンネル内で利用し、PeerCastStation の管理 API や HLS URL をインターネットに公開しないでください。JSON のプロパティ名は camelCase、時刻は UTC の ISO 8601 です。

## ペアリング

Windows の「ペアリングQRを作成」が次の JSON を表示します。`pairCode` は5分・1回限りです。

```json
{"v":1,"baseUrl":"http://100.101.102.103:17444","pairCode":"...","expiresAt":"2026-09-23T12:00:00+00:00"}
```

`POST {baseUrl}/api/v1/pair` に `{"pairCode":"..."}` を送ります。成功時は `200 {"token":"...","tokenType":"Bearer"}`。誤り・期限切れは `401 {"error":"invalid_or_expired_pair_code"}` です。以後の API 要求に `Authorization: Bearer <token>` を付けます。Windows 側で再ペアリングまたは登録解除すると古いトークンは無効になり、現在の視聴も終了します。登録できる資格情報は1台分です。

## 番組の開始と状態

スマホ側の既存 `Channel` モデルから、FLV 番組の32桁16進 ID と `tracker` の `host:port` を送ります。Windows経由の視聴時、既定のSP一覧だけはWindows側で取得し、それ以外のYPはスマホ側で取得します。

`GET /api/v1/sp/index.txt`（Bearer認証必須）は、Windowsから固定URL `http://bayonet.ddo.jp/sp/index.txt` を取得した内容を返します。SPはWindowsの外部IPでポート判定するため、PeerCastStationの外部リレーポートを開放してください。応答は最大4 MiBで、上流が失敗すると`502`です。APIに任意URLは渡せません。スマホからの直接取得にフォールバックすると判定元が変わるため、失敗時は一覧取得エラーとして扱います。SPの使用ポートを変更する場合やSP自身の詳細な「Port check」を見る場合は、Windows PCのブラウザでSPを開いてください。

```http
POST /api/v1/sessions
Authorization: Bearer <token>
Content-Type: application/json

{"channelId":"0123456789ABCDEF0123456789ABCDEF","tracker":"example.net:7144"}
```

成功は `202 {"sessionId":"...","state":"starting"}`。同じ番組の再要求は同じセッションを返します。別番組が使用中なら `409 {"error":"session_busy","sessionId":"..."}`、入力が不正なら `422 {"error":"invalid_channel_or_tracker"}` です。認証失敗は `401` です。

`GET /api/v1/sessions/current`（または `/api/v1/status`）を準備中に1秒程度でポーリングします。返却例:

```json
{
  "sessionId":"...", "channelId":"0123456789ABCDEF0123456789ABCDEF",
  "state":"ready", "error":null,
  "peerCastOnline":true, "relayReachable":true, "downstreamRelays":0,
  "relayMessage":"リレー待受可能・下流 0",
  "playlists":{
    "auto":"http://100.101.102.103:17444/hls/.../master.m3u8",
    "high":"http://100.101.102.103:17444/hls/.../high/index.m3u8",
    "medium":"http://100.101.102.103:17444/hls/.../medium/index.m3u8",
    "low":"http://100.101.102.103:17444/hls/.../low/index.m3u8"
  }
}
```

状態は `idle`、`starting`、`ready`、`failed`。`failed` の場合は `error` を表示し、再要求できます。`playlists` は `ready` まで `null` です。画質「自動」は `auto`、固定の高・中・低は対応する URL を再生します。画質を変える際は再生 URL を切り替えてください。HLS の子プレイリストとセグメントは相対参照なので、URL を書き換えずに使います。

HLS URL に認証情報が含まれます。ログ、解析サービス、共有画面に記録せず、端末内の安全な保存領域だけにトークンを保存してください。HLS 要求が2分間なければ Windows が変換を終了します。明示的な終了は `DELETE /api/v1/sessions/current`（成功 `204`、既に終了していても `204`）を送ります。

映像は H.264/AAC の MPEG-TS HLS、3秒セグメント、6セグメントのライブ窓です。高は最大720p/映像2.5 Mbps、中は480p/1.2 Mbps、低は360p/0.5 Mbps です。ソースを拡大しません。音声のない FLV は映像のみで配信します。映像のない配信は `failed` になります。

VPN 切断中は再生できません。復帰後は状態を再取得し、`ready` なら新しいプレイリスト要求で再開してください。PeerCastStation のポートが閉じている場合は `relayReachable=false` を表示し、HLS 視聴そのものは妨げません。

現行スマホアプリは Android の cleartext 通信を許可先に限定しています。連携機能を実装する際は、Tailscale 内の HTTP API と HLS を Android/iOS のネットワークポリシーで利用できるようにしてください。Windows 側の待受を公開インターネットへ向けたり、PeerCastStation の管理 API をスマホへ直接開いたりしないでください。
