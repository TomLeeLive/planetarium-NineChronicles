using System;
using Nekoyume.L10n;
using Nekoyume.UI.Module;

namespace Nekoyume.UI.Model
{
    using UniRx;

    public class ItemCountPopup<T> : IDisposable where T : ItemCountPopup<T>
    {
        private int _originalCount;

        public readonly ReactiveProperty<string> TitleText = new ReactiveProperty<string>("");
        public readonly ReactiveProperty<CountEditableItem> Item = new ReactiveProperty<CountEditableItem>(null);
        public readonly ReactiveProperty<bool> CountEnabled = new ReactiveProperty<bool>(true);
        public readonly ReactiveProperty<string> SubmitText = new ReactiveProperty<string>("");
        public readonly ReactiveProperty<bool> Submittable = new ReactiveProperty<bool>(true);
        public readonly ReactiveProperty<string> InfoText = new ReactiveProperty<string>("");
        public readonly Subject<(ConditionalButton.State, T)> OnClickConditional = new();
        public readonly Subject<T> OnClickCancel = new Subject<T>();

        public ItemCountPopup()
        {
            SubmitText.Value = L10nManager.Localize("UI_OK");

            Item.Subscribe(value =>
            {
                if (ReferenceEquals(value, null))
                {
                    _originalCount = 0;
                    return;
                }

                _originalCount = value.Count.Value;
            });

            OnClickCancel.Subscribe(value =>
            {
                if (value.Item.Value is not null)
                {
                    value.Item.Value.Count.Value = _originalCount;
                }
            });
        }

        public virtual void Dispose()
        {
            TitleText.Dispose();
            Item.Dispose();
            CountEnabled.Dispose();
            SubmitText.Dispose();
            Submittable.Dispose();
            InfoText.Dispose();
            OnClickConditional.Dispose();
            OnClickCancel.Dispose();
        }
    }
}
