using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class IntroScreen : LoadingScreen
    {
        [Header("Mobile")]
        [SerializeField] private GameObject mobileContainer;
        [SerializeField] private Button touchScreenButton;

        [SerializeField] private GameObject startButtonContainer;
        [SerializeField] private Button startButton;
        [SerializeField] private Button signinButton;

        [SerializeField] private GameObject socialLoginContainer;
        [SerializeField] private CapturedImage socialLoginBackground;
        [SerializeField] private Button googleLoginButton;
        [SerializeField] private Button twitterLoginButton;
        [SerializeField] private Button discordLoginButton;
        [SerializeField] private Button appleLoginButton;

        [SerializeField] private GameObject qrCodeGuideContainer;
        [SerializeField] private CapturedImage qrCodeGuideBackground;
        [SerializeField] private GameObject[] qrCodeGuideImages;
        [SerializeField] private TextMeshProUGUI qrCodeGuideText;
        [SerializeField] private Button qrCodeGuideNextButton;

        [SerializeField] private SocialLogin socialLogin;

        private int _guideIndex = 0;
        private const int GuideCount = 3;

        private string _keyStorePath;
        private string _privateKey;

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();

            touchScreenButton.onClick.AddListener(() =>
            {
                touchScreenButton.gameObject.SetActive(false);
                startButtonContainer.SetActive(true);
            });
            startButton.onClick.AddListener(() =>
            {
                startButtonContainer.SetActive(false);
                socialLoginBackground.Show();
                socialLoginContainer.SetActive(true);
            });
            signinButton.onClick.AddListener(() =>
            {
                startButtonContainer.SetActive(false);
                qrCodeGuideBackground.Show();
                qrCodeGuideContainer.SetActive(true);
                foreach (var image in qrCodeGuideImages)
                {
                    image.SetActive(false);
                }

                _guideIndex = 0;
                ShowQrCodeGuide();
            });
            qrCodeGuideNextButton.onClick.AddListener(() =>
            {
                _guideIndex++;
                ShowQrCodeGuide();
            });

            googleLoginButton.onClick.AddListener(() =>
            {
                socialLoginContainer.SetActive(false);
                Find<GrayLoadingScreen>().Show("Sign in...", false);
                socialLogin.Signin(() =>
                {
                    Find<LoginSystem>().Show(_keyStorePath, _privateKey);
                });
            });
            twitterLoginButton.onClick.AddListener(() =>
            {
                socialLoginContainer.SetActive(false);
                Find<GrayLoadingScreen>().Show("Sign in...", false);

                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            });
            discordLoginButton.onClick.AddListener(() =>
            {
                socialLoginContainer.SetActive(false);
                Find<GrayLoadingScreen>().Show("Sign in...", false);

                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            });
            appleLoginButton.onClick.AddListener(() =>
            {
                socialLoginContainer.SetActive(false);
                Find<GrayLoadingScreen>().Show("Sign in...", false);

                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            });

            touchScreenButton.interactable = true;
            startButton.interactable = true;
            signinButton.interactable = true;
            qrCodeGuideNextButton.interactable = true;
            googleLoginButton.interactable = true;
            twitterLoginButton.interactable = true;
            discordLoginButton.interactable = true;
            appleLoginButton.interactable = true;
        }

        public void Show(string keyStorePath, string privateKey)
        {
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);

            if (Platform.IsMobilePlatform())
            {
                mobileContainer.SetActive(true);
                startButtonContainer.SetActive(false);
                socialLoginContainer.SetActive(false);
                qrCodeGuideContainer.SetActive(false);
            }
            else
            {
                mobileContainer.SetActive(false);

                indicator.Show("Verifying transaction..");
                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            }
        }

        private void ShowQrCodeGuide()
        {
            if (_guideIndex >= GuideCount)
            {
                qrCodeGuideContainer.SetActive(false);

                Find<LoginSystem>().Show(_keyStorePath, _privateKey);
            }
            else
            {
                qrCodeGuideImages[_guideIndex].SetActive(true);
                qrCodeGuideText.text = L10nManager.Localize($"INTRO_QR_CODE_GUIDE_{_guideIndex}");
            }
        }
    }
}
