using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aquamarine.Source.Networking;

public partial class WebRtcMultiplayerClient : WebsocketSignalingClient
{
	private WebRtcMultiplayerPeer rtcPeer = new WebRtcMultiplayerPeer();
	private bool rtcSealed = false;

	public WebRtcMultiplayerClient() {
		Connect(nameof(ConnectedEventHandler), new Callable(this, nameof(Connected)));
		Connect(nameof(DisconnectedEventHandler), new Callable(this, nameof(Disconnected)));
		
		Connect(nameof(OfferReceivedEventHandler), new Callable(this, nameof(OfferReceived)));
		Connect(nameof(AnswerRecivedEventHandler), new Callable(this, nameof(AnswerRecived)));
		Connect(nameof(CandidateRecivedEventHandler), new Callable(this, nameof(CandidateRecived)));
		
		Connect(nameof(LobbyJoinedEventHandler), new Callable(this, nameof(LobbyJoined)));
		Connect(nameof(LobbySealedEventHandler), new Callable(this, nameof(LobbySealed)));
		Connect(nameof(PeerConnectedEventHandler), new Callable(this, nameof(PeerConnected)));
		Connect(nameof(PeerDisconnected), new Callable(this, nameof(PeerDisconnected)));
	}

	public void Start(string url, string _lobby="", bool _mesh=true) {
		Stop();
		rtcSealed = false;
		mesh = _mesh;
		lobby = _lobby;

		ConnectToUrl(url);
	}

	public void Stop() {
		Multiplayer.MultiplayerPeer = null;
		rtcPeer.Close();
		Close();
	}

	private WebRtcPeerConnection CreatePeer(int id) {
		WebRtcPeerConnection peer = new WebRtcPeerConnection();

		var iceServers = new Godot.Collections.Dictionary<string, Variant> {
            { "urls", new string[] { "stun:stun.l.google.com:19302" } }
        };

		peer.Initialize((Godot.Collections.Dictionary)new Godot.Collections.Dictionary<string, Variant> { {"iceServers", iceServers} });
		peer.SessionDescriptionCreated += (type, sdp) => OfferCreated(type, sdp, id);
		peer.IceCandidateCreated += (mid, index, name) => NewIceCandadte(mid, (int)index, name, id); // NOTE: Keep and eye on this skechy type cast :3c

		rtcPeer.AddPeer(peer, id);
		if (id < rtcPeer.GenerateUniqueId()) {
			peer.CreateOffer();
		}

		return peer;
	}

	private void NewIceCandadte(string midName, int indexName, string sdpName, int id) {
		SendCandidate(id, midName, indexName, sdpName);
	}

	private void OfferCreated(string type, string data, int id) {
		if (!rtcPeer.HasPeer(id)) {
			return;
		}

		GD.Print("Created new WebRTC offer with type", type);
		rtcPeer.GetPeer(id)["connection"].As<WebRtcPeerConnection>().SetLocalDescription(type, data);
		
		if (type == "offer") {
			SendOffer(id, data);
		} else {
			SendAnswer(id, data);
		}
	}

	private new void Connected(int id, bool useMesh) {
		GD.Print($"Connected {id}; Using mesh:{mesh}");

		if (useMesh) {
			rtcPeer.CreateMesh(id);
		} else if (id == 1) {
			rtcPeer.CreateServer();
		} else {
			rtcPeer.CreateClient(id);
		}

		Multiplayer.MultiplayerPeer = rtcPeer;
	}

	private new void LobbyJoined(string _lobby) {
		lobby = _lobby;
	}

	private new void LobbySealed() {
		rtcSealed = true;
	}

	private new void Disconnected() {
		GD.Print($"Disconnected WSCCODE_{code}: \"{reason}\"");

		if (!rtcSealed) {
			GD.PrintErr("A non-gracefull disconnect occured, Cleaning up.");
			Stop();
		}
	}

	private new void PeerConnected(int id) {
		GD.Print($"Peer connected: {id}");

		CreatePeer(id);
	}

	private new void PeerDisconnected(int id) {
		GD.Print($"Peer disconnected: {id}");

		if (rtcPeer.HasPeer(id)) {
			rtcPeer.RemovePeer(id);
		}
	}

	private new void OfferReceived(int id, string offer) {
		GD.Print($"New offer: {id}");
		if (rtcPeer.HasPeer(id)) {
			rtcPeer.GetPeer(id)["connection"].As<WebRtcPeerConnection>().SetLocalDescription("offer", offer);
		}
	}

	private new void AnswerRecived(int id, string answer) {
		GD.Print($"Recived answer: {id}");
		if (rtcPeer.HasPeer(id)) {
			rtcPeer.GetPeer(id)["connection"].As<WebRtcPeerConnection>().SetLocalDescription("answer", answer);
		}
	}

	private new void CandidateRecived(int id, string mid, int index, string sdp) {
		if (rtcPeer.HasPeer(id)) {
			rtcPeer.GetPeer(id)["connection"].As<WebRtcPeerConnection>().AddIceCandidate(mid, index, sdp);
		}
	}
}
