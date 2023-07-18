using Libplanet;
using Libplanet.Blocks;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class VersionSystem : SystemWidget
    {
        public TextMeshProUGUI informationText;
        private int _version;
        private long _blockIndex;
        private BlockHash _hash;
        private string _clientCommitHash;

        protected override void Awake()
        {
            base.Awake();
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(SubscribeBlockIndex).AddTo(gameObject);
            Game.Game.instance.Agent.BlockTipHashSubject.Subscribe(SubscribeBlockHash).AddTo(gameObject);

            _clientCommitHash = Resources.Load<TextAsset>("ClientHash")?.text[..8] ?? string.Empty;

// #if UNITY_ANDROID || UNITY_IOS
            UpdateText();
// #endif
        }

        public void SetVersion(int version)
        {
            _version = version;
            UpdateText();
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            UpdateText();
        }

        private void SubscribeBlockHash(BlockHash hash)
        {
            _hash = hash;
            UpdateText();
        }

        private void UpdateText()
        {
            var hash = _hash.ToString();
            hash = hash.Length >= 4 ? hash.Substring(0, 4) : "...";

#if UNITY_ANDROID || UNITY_IOS
            informationText.text = $"APV: {_version} / #{_blockIndex} / Hash: {hash} / ver: {UnityEngine.Application.version}({_clientCommitHash})";
#else
            informationText.text = $"APV: {_version} / #{_blockIndex} / Hash: {hash} / ver: {_clientCommitHash}";
#endif
        }
    }
}
