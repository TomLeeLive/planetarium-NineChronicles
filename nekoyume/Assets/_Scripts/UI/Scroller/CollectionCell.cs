using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class CollectionCell : RectCell<CollectionModel, CollectionScroll.ContextModel>
    {
        [Serializable]
        private struct ActiveButtonLoading
        {
            public GameObject container;
            public GameObject indicator;
            public GameObject text;
        }

        [SerializeField] private CollectionStat complete;
        [SerializeField] private CollectionStat incomplete;
        [SerializeField] private CollectionItemView[] collectionItemViews;
        [SerializeField] private ConditionalButton activeButton;
        [SerializeField] private ActiveButtonLoading activeButtonLoading;

        private CollectionModel _itemData;
        private readonly List<IDisposable> _disposables = new();

        private void Awake()
        {
            activeButton.OnSubmitSubject
                .Subscribe(_=> Context.OnClickActiveButton.OnNext(_itemData))
                .AddTo(gameObject);
            activeButton.OnClickDisabledSubject
                .Where(_ => LoadingHelper.ActivateCollection.Value != 0)
                .Subscribe(_ =>
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("NOTIFICATION_COLLECTION_DISABLED_ACTIVATING"),
                        NotificationCell.NotificationType.Information))
                .AddTo(gameObject);
        }

        public override void UpdateContent(CollectionModel itemData)
        {
            _itemData = itemData;

            complete.gameObject.SetActive(false);
            incomplete.gameObject.SetActive(false);

            var cellBackground = itemData.Active ? complete : incomplete;
            cellBackground.Set(itemData);

            var materialCount = itemData.Row.Materials.Count;

            for (var i = 0; i < collectionItemViews.Length; i++)
            {
                collectionItemViews[i].gameObject.SetActive(i < materialCount);
                if (i >= materialCount)
                {
                    continue;
                }

                collectionItemViews[i].Set(
                    itemData.Materials[i],
                    model => Context.OnClickMaterial.OnNext(model));
            }

            activeButton.SetCondition(() => itemData.CanActivate);
            activeButton.Interactable = !itemData.Active;

            _disposables.DisposeAllAndClear();
            LoadingHelper.ActivateCollection.Subscribe(collectionId =>
            {
                var loading = collectionId != 0;
                if (loading)
                {
                    var inProgress = collectionId == _itemData.Row.Id;
                    activeButtonLoading.indicator.SetActive(inProgress);
                    activeButtonLoading.text.SetActive(!inProgress);
                }

                activeButton.Interactable = !loading && !_itemData.Active;
                activeButtonLoading.container.SetActive(loading);
            }).AddTo(_disposables);
        }
    }
}
