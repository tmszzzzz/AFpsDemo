using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(ClientGame))]
    public class NetDebugUI : MonoBehaviour
    {
        private ClientGame _clientGame;

        private void Awake()
        {
            _clientGame = GetComponent<ClientGame>();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 320, 120), GUI.skin.box);

            GUILayout.Label($"Connected: {_clientGame.IsJoined}");
            GUILayout.Label($"PlayerId: {_clientGame.PlayerId}");
            GUILayout.Label($"Ping: {_clientGame.LastPingMs} ms");
            GUILayout.Label($"ServerTime: {_clientGame.LastServerTime} ms");

            if (!string.IsNullOrEmpty(_clientGame.LastError))
            {
                GUILayout.Label($"Error: {_clientGame.LastError}");
            }

            GUILayout.EndArea();
        }
    }
}