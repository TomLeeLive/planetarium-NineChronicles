﻿using System.Linq;
using Nekoyume.Game;
using Nekoyume.State;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.UI.Module
{
    public class Mileage : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI amountText;

        [SerializeField]
        private GameObject loadingObject;

        private Coroutine _request;

        private void OnEnable()
        {
            if (Dcc.instance.Avatars is null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (States.Instance.AgentState is null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (States.Instance.AgentState.avatarAddresses.Values.Any(addr =>
                    Dcc.instance.Avatars.TryGetValue(addr.ToString(), out _)))
            {
                loadingObject.SetActive(true);
                amountText.gameObject.SetActive(false);
                var url = $"{Game.Game.instance.URL.DccMileageAPI}{States.Instance.AgentState.address}";
                var headerName = Game.Game.instance.URL.DccEthChainHeaderName;
                var headerValue = Game.Game.instance.URL.DccEthChainHeaderValue;
                _request = StartCoroutine(RequestManager.instance.GetJson(
                    url,
                    headerName,
                    headerValue,
                    (json) =>
                {
                    var mileage = (int)(JObject.Parse(json)["mileage"]?.ToObject<decimal>() ?? 0);
                    amountText.text = mileage.ToCurrencyNotation();
                    loadingObject.SetActive(false);
                    amountText.gameObject.SetActive(true);
                }));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (_request is not null)
            {
                StopCoroutine(_request);
                _request = null;
            }
        }
    }
}
