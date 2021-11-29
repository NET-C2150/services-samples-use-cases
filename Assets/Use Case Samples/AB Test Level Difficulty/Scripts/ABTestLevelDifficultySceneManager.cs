using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace GameOperationsSamples
{
    namespace ABTestLevelDifficulty
    {
        public class ABTestLevelDifficultySceneManager : MonoBehaviour
        {
            public ABTestLevelDifficultySampleView sceneView;

            void OnEnable()
            {
                StartSubscribe();
            }

            void OnDisable()
            {
                StopSubscribe();
            }

            void StartSubscribe()
            {
                CloudCodeManager.LeveledUp += OpenLeveledUpPopup;
            }

            void StopSubscribe()
            {
                CloudCodeManager.LeveledUp -= OpenLeveledUpPopup;
            }

            async void Start()
            {
                try
                {
                    await InitializeServices();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            async Task InitializeServices()
            {
                await UnityServices.InitializeAsync();

                // Check that scene has not been unloaded while processing async wait to prevent throw.
                if (this == null) return;

                // Analytics events must be sent after UnityServices.Initialize() is finished.
                AnalyticsManager.instance.SendSceneOpenedEvent();

                await SignInAndLoadDataFromServices();
                if (this == null) return;

                Debug.Log("Initialization and signin complete.");
            }

            async Task SignInAndLoadDataFromServices()
            {
                await SignInIfNecessary();
                if (this == null) return;
                Debug.Log($"Player id: {AuthenticationService.Instance.PlayerId}");

                await LoadServicesData();
                if (this == null) return;

                UpdateSceneViewAfterSignIn();
            }

            async Task SignInIfNecessary()
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("Signing in...");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }

            async Task LoadServicesData()
            {
                await Task.WhenAll(
                    CloudSaveManager.instance.LoadAndCacheData(),
                    EconomyManager.instance.RefreshCurrencyBalances(),
                    RemoteConfigManager.instance.FetchConfigs()
                );
            }

            void UpdateSceneViewAfterSignIn()
            {
                sceneView.OnSignedIn();
                sceneView.EnableAndUpdate();
            }

            public async void OnSignInAsNewUserButtonPressed()
            {
                try
                {
                    AnalyticsManager.instance.SendActionButtonPressedEvent("SignInAsNewUser");

                    SignOut();
                    if (this == null) return;

                    await SignInAndLoadDataFromServices();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            void SignOut()
            {
                // Note that signing out here signs you out of this player ID across all the use case samples.
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("Signing out current player...");
                    CloudSaveManager.instance.ClearCachedData();
                    RemoteConfigManager.instance.ClearCachedData();
                    EconomyManager.instance.ClearCurrencyBalances();

                    AuthenticationService.Instance.SignOut();
                    UpdateSceneViewAfterSignOut();
                }
            }

            void UpdateSceneViewAfterSignOut()
            {
                sceneView.OnSignedOut();
                sceneView.UpdateScene();
            }

            public async void OnGainXPButtonPressed()
            {
                try
                {
                    AnalyticsManager.instance.SendActionButtonPressedEvent("GainXP");

                    await CloudCodeManager.instance.CallGainXPAndLevelIfReadyEndpoint();
                    if (this == null) return;

                    sceneView.UpdateScene();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            void OpenLeveledUpPopup(string currencyId, long rewardQuantity)
            {
                sceneView.UpdateScene();

                string spriteAddress = default;

                // Convert the currency ID (ex: "COIN") to Addressable address (ex: "Sprites/Currency/Coin")
                if (RemoteConfigManager.instance.currencyDataDictionary.TryGetValue(currencyId, out var currencyData))
                {
                    spriteAddress = currencyData.spriteAddress;
                }

                var rewards = new List<RewardDetail>();
                rewards.Add(new RewardDetail { 
                    id = currencyId,
                    spriteAddress = spriteAddress, 
                    quantity = rewardQuantity 
                });

                sceneView.OpenLevelUpPopup(rewards);
            }
        }
    }
}
