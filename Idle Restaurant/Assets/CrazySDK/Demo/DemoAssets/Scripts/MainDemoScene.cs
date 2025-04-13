using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyGames
{
    public class MainDemoScene : MonoBehaviour
    {
        public Text adblockText;
        public Text infoText;
        public GameObject initializedPanel,
            notInitializedPanel;

        private void Start()
        {
            initializedPanel.SetActive(false);
            notInitializedPanel.SetActive(true);
            CrazySDK.Init(() =>
            {
                infoText.text = $"SDK Version: {CrazySDK.Version} \n";
                infoText.text += $"SDK Initialized: {CrazySDK.IsInitialized} \n";
                infoText.text += $"Is QA Tool: {CrazySDK.IsQaTool} \n";
                infoText.text += $"Is user account available: {CrazySDK.User.IsUserAccountAvailable} \n";
                infoText.text += $"Environment: {CrazySDK.Environment} \n";
                infoText.text += $"Is SDK Available: {CrazySDK.IsAvailable} \n";

                CrazySDK.User.GetUser(
                    (
                        user =>
                        {
                            Debug.Log(("Got user" + user));
                        }
                    )
                );
                CrazySDK.Ad.HasAdblock(
                    (adblockPresent) =>
                    {
                        infoText.text += "Has adblock: " + adblockPresent;
                    }
                );

                StartCoroutine(StartAdblockStatusMonitoring());

                initializedPanel.SetActive(true);
                notInitializedPanel.SetActive(false);
            });
        }

        IEnumerator StartAdblockStatusMonitoring()
        {
            while (true)
            {
                adblockText.text = "Adblock status: " + CrazySDK.Ad.AdblockStatus;
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
