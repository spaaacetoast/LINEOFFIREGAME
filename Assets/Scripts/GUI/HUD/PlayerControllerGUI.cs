using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using AngryRain.Multiplayer;

namespace AngryRain
{
    public class PlayerControllerGUI : MonoBehaviour
    {
        public static List<PlayerControllerGUI> allInstances = new List<PlayerControllerGUI>();

        public LocalPlayer localPlayer { get; set; }
        public GameObject guiCamera;

        #region elements

        public ElementHolder elements = new ElementHolder();
        [System.Serializable]
        public class ElementHolder
        {
            public GameObject textFeedObject;
            public GameObject killTextObject;
            public GameObject objectiveObject;
            public GameObject nametagObject;
        }

        #endregion

        #region Components

        public CanvasGroup canvasGroup { private set; get; }

        public Crosshair crosshair { private set; get; }

        public PlayerInfo playerInfo { private set; get; }

        public GameObject hitmarker { private set; get; }

        public Image hitEffectFull { private set; get; }
        public Image fadeBlack { private set; get; }

        public MenuSettings currentMenu { private set; get; }
        public MenuSettings[] allMenus;

        #endregion

        #region static

        public static PlayerControllerGUI CreatePlayerGUI(int index)
        {
            if (allInstances.Count > index)
                return allInstances[index];

            GameObject go = Instantiate(allInstances[0].gameObject);
            PlayerControllerGUI pc = go.GetComponent<PlayerControllerGUI>();
            pc.localPlayer = LocalPlayerManager.localPlayers[index];
            allInstances.Add(pc);
            return pc;

            //TODO, DUPLICATE CAMERA AND GIVE CORRECT RECT
        }

        #endregion

        void Awake()
        {
            if (!allInstances.Contains(this))
                allInstances.Add(this);

            crosshair = transform.Find("Player/GUI/Crosshair").GetComponent<Crosshair>();
            playerInfo = transform.Find("Player/GUI/Player Info").GetComponent<PlayerInfo>();
            hitmarker = transform.Find("Player/Hitmarker").gameObject;
            hitEffectFull = transform.Find("Player/Hit Effect/Full").GetComponent<Image>();
            fadeBlack = transform.Find("FadeBlack").GetComponent<Image>();
            canvasGroup = transform.Find("Menus").GetComponent<CanvasGroup>();

            InitializeDeathScreen();

            GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10;
            playerInfo.Initialize();
        }

        void Start()
        {
            if (allInstances[0] == this)
                localPlayer = LocalPlayerManager.localPlayers[0];

            hitmarker.SetActive(false);
            playerInfo.gameObject.SetActive(false);
            crosshair.gameObject.SetActive(false);
            hitEffectFull.color = new Color(1, 0.35f, 0, 0);
        }

        void OnDestroy()
        {
            allInstances.Remove(this);
        }

        void Update()
        {
            UpdateTextFeedController();
            UpdateKillFeedController();
        }

        #region text feed controller

        public List<TextObject> allTextObjects = new List<TextObject>();
        public class TextObject
        {
            public GameObject gameObject;
            public RectTransform rect;
            public Text text;

            public float creationTime;
            public float deletionTime;

            public float currentHeight;
            public float currentWidth;
        }

        void UpdateTextFeedController()
        {
            for(int i = 0; i < allTextObjects.Count; i++)
            {
                //Fade in after creation
                if (Time.time - allTextObjects[i].creationTime < 1)
                {
                    allTextObjects[i].text.color = new Color(1, 1, 1, Time.time - allTextObjects[i].creationTime);
                    allTextObjects[i].currentHeight = Mathf.Lerp(0, 16, Time.time - allTextObjects[i].creationTime);
                }

                //Start fading the text when its 1 second before deletion time
                if (Time.time - allTextObjects[i].deletionTime + 1 > 0)
                {
                    allTextObjects[i].text.color = new Color(1, 1, 1, 1 - (Time.time - allTextObjects[i].deletionTime + 1));
                    allTextObjects[i].currentHeight = Mathf.Lerp(0, 16, 1 - (Time.time - allTextObjects[i].deletionTime + 1));
                }

                //Get all the texts before this one and add up all teh ehights
                float totalHeight = 0;
                for (int x = 0; x < i; x++)
                {
                    totalHeight += allTextObjects[x].currentHeight;
                }
                allTextObjects[i].rect.anchoredPosition = new Vector2(0, -totalHeight);
                allTextObjects[i].rect.localPosition = new Vector3(allTextObjects[i].rect.localPosition.x, allTextObjects[i].rect.localPosition.y, 0);

                //Delete the object when deletion time is reached
                if (Time.time - allTextObjects[i].deletionTime > 0)
                {
                    Destroy(allTextObjects[i].gameObject);
                    allTextObjects.Remove(allTextObjects[i]);
                }
            }
        }

        public void AddText(string text)
        {
            TextObject to = new TextObject();
            GameObject go = Instantiate(elements.textFeedObject) as GameObject;

            to.gameObject = go;
            to.text = go.GetComponent<Text>();
            to.rect = go.GetComponent<RectTransform>();

            allTextObjects.Add(to);
            int index = allTextObjects.IndexOf(to);

            go.transform.SetParent(elements.textFeedObject.transform.parent, false);
            //go.transform.parent = elements.textFeedObject.transform.parent;

            to.rect.localScale = new Vector3(1f, 1f, 1f);
            to.rect.localRotation = Quaternion.identity;
            to.rect.position = new Vector3(0, index * 16, 0);

            to.text.text = text;

            to.creationTime = Time.time;
            to.deletionTime = Time.time + 6;

            go.SetActive(true);
        }

        #endregion

        #region kill feed controller

        public List<TextObject> allKillTextObjects = new List<TextObject>();

        void UpdateKillFeedController()
        {
            for (int i = 0; i < allKillTextObjects.Count; i++)
            {
                //Fade in after creation
                if (Time.time - allKillTextObjects[i].creationTime < 1)
                {
                    allKillTextObjects[i].text.color = new Color(1, 1, 1, Time.time - allKillTextObjects[i].creationTime);
                    allKillTextObjects[i].currentWidth = Mathf.Lerp(0, 200, (Time.time - allKillTextObjects[i].creationTime) * 5);
                    allKillTextObjects[i].currentHeight = Mathf.Lerp(0, 20, (Time.time - allKillTextObjects[i].creationTime)*10);
                    allKillTextObjects[i].rect.sizeDelta = new Vector2(allKillTextObjects[i].currentWidth, 20);
                }

                //Start fading the text when its 1 second before deletion time
                if (Time.time - allKillTextObjects[i].deletionTime + 1 > 0)
                {
                    allKillTextObjects[i].text.color = new Color(1, 1, 1, 1 - (Time.time - allKillTextObjects[i].deletionTime + 1));
                    allKillTextObjects[i].currentWidth = Mathf.Lerp(0, 200, 1 - (Time.time - allKillTextObjects[i].deletionTime + 1));
                    allKillTextObjects[i].currentHeight = Mathf.Lerp(0, 20, 1 - (Time.time - allKillTextObjects[i].deletionTime) * 3);
                    allKillTextObjects[i].rect.sizeDelta = new Vector2(allKillTextObjects[i].currentWidth, 20);
                }

                //Get all the texts before this one and add up all teh ehights
                float totalHeight = 0;
                for (int x = 0; x < i; x++)
                {
                    totalHeight += allKillTextObjects[x].currentHeight;
                }
                allKillTextObjects[i].rect.anchoredPosition = new Vector2(0, -totalHeight);
                allKillTextObjects[i].rect.localPosition = new Vector3(allKillTextObjects[i].rect.localPosition.x, allKillTextObjects[i].rect.localPosition.y, 0);

                //Delete the object when deletion time is reached
                if (Time.time - allKillTextObjects[i].deletionTime > 1)
                {
                    Destroy(allKillTextObjects[i].gameObject);
                    allKillTextObjects.Remove(allKillTextObjects[i]);
                }
            }
        }

        public void AddKillText(int killType)
        {
            string text = "";

            switch(killType)
            {
                case 0:
                    text = "+10 ENEMY KILLED";
                    break;
                case 1:
                    text = "+2 HEADSHOT";
                    AddKillText(0);
                    break;
                case 2:
                    text = "+10 GRENADE KILL";
                    break;
                case 3:
                    text = "SUICIDE";
                    break;
            }

            TextObject to = new TextObject();
            GameObject go = Instantiate(elements.killTextObject) as GameObject;

            to.gameObject = go;
            to.text = go.transform.Find("Text").GetComponent<Text>();
            to.rect = go.GetComponent<RectTransform>();

            allKillTextObjects.Add(to);
            int index = allKillTextObjects.IndexOf(to);

            go.transform.SetParent(elements.killTextObject.transform.parent, false);
            //go.transform.parent = elements.textFeedObject.transform.parent;

            to.rect.localScale = new Vector3(1f, 1f, 1f);
            to.rect.localRotation = Quaternion.identity;
            to.rect.localPosition = new Vector3(0, index * 20, 0);
            to.rect.sizeDelta = new Vector2(0, 20);

            to.text.text = text;

            to.creationTime = Time.time;
            to.deletionTime = Time.time + 6;

            go.SetActive(true);
        }

        #endregion

        #region Menu Navigation

        public void NavigateTo(string nextMenu)
        {
            MenuSettings ms = null;

            for (int i = 0; i < allMenus.Length; i++)
                if (allMenus[i].menuName.Equals(nextMenu, System.StringComparison.InvariantCultureIgnoreCase))
                    ms = allMenus[i];

            StopCoroutine("NavigateAnimation");
            StartCoroutine(NavigateAnimation(ms));
        }

        IEnumerator NavigateAnimation(MenuSettings nextMenu)
        {
            float startTime = Time.time;
            if (currentMenu != null)
            {
                if (currentMenu.fadeToBlackOnTransition || nextMenu != null && nextMenu.fadeToBlackOnTransition)
                    FullscreenFade(true);

                while (Time.time - startTime <= 0.6f)
                {
                    float t = Time.time - startTime;
                    canvasGroup.alpha = curve.Evaluate(1 - (t * 3));
                    canvasGroup.GetComponent<RectTransform>().localScale = Vector3.Lerp(Vector3.one * 0.975f, Vector3.one, curve.Evaluate(1 - (t * 3)));
                    yield return new WaitForEndOfFrame();
                }

                currentMenu.menuObject.SetActive(false);

                if (currentMenu.fadeToBlackOnTransition || nextMenu != null && nextMenu.fadeToBlackOnTransition)
                    FullscreenFade(false);
            }

            DisableAllMenus();
            currentMenu = nextMenu;

            if (currentMenu != null)
            {
                transform.Find("Menus/main").gameObject.SetActive(currentMenu.mainBarEnabled);
                currentMenu.menuObject.SetActive(true);

                startTime = Time.time;
                while (Time.time - startTime <= 0.5f)
                {
                    float t = Time.time - startTime;
                    canvasGroup.alpha = curve.Evaluate(t * 4);
                    canvasGroup.GetComponent<RectTransform>().localScale = Vector3.Lerp(Vector3.one * 1.025f, Vector3.one, curve.Evaluate(t * 2));
                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
                transform.Find("Menus/main").gameObject.SetActive(false);
            }
        }

        public void DisableAllMenus()
        {
            StopCoroutine("NavigateAnimation");
            for (int i = 0; i < allMenus.Length; i++)
                allMenus[i].menuObject.SetActive(false);
            currentMenu = null;
        }

        [System.Serializable]
        public class MenuSettings
        {
            public string menuName;
            public GameObject menuObject;

            public bool fadeToBlackOnTransition;
            public bool mainBarEnabled = true;
        }

        #endregion

        #region Events

        public void Event_SpawnLocalPlayer()
        {
            NavigateTo("");
            StartCoroutine("DelayedSpawning", 0.5f);
            FullscreenFade(true);
        }

        IEnumerator DelayedSpawning(float delay)
        {
            yield return new WaitForSeconds(delay);
            Multiplayer.MultiplayerManager.instance.Local_RequestPlayerSpawn(localPlayer.playerIndex);
        }

        #endregion

        #region Hitmarker

        //Hitmarker handler
        public void EnableHitmarker()
        {
            StopCoroutine(HandleHitmarker());
            StartCoroutine(HandleHitmarker());
        }

        IEnumerator HandleHitmarker()
        {
            hitmarker.SetActive(false);
            yield return new WaitForEndOfFrame();
            hitmarker.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            hitmarker.SetActive(false);
        }

        //Fullscreen hit effect()
        bool isHitEffectActive;
        float hitEffectStrength;

        public void EnableGitHitEffect(float strength = 0.5f)
        {
            hitEffectStrength += strength;
            if (!isHitEffectActive)
                StartCoroutine(HandleGetHitEffect());
        }

        IEnumerator HandleGetHitEffect()
        {
            hitEffectFull.enabled = true;
            isHitEffectActive = true;
            while (isHitEffectActive)
            {
                hitEffectFull.color = new Color(1, 0.35f, 0, hitEffectStrength / 2);
                hitEffectStrength = Mathf.MoveTowards(hitEffectStrength, 0, Time.deltaTime * 2);
                yield return new WaitForEndOfFrame();
                if (System.Math.Abs(hitEffectStrength) < 0.1f)
                    isHitEffectActive = false;
            }
            hitEffectFull.enabled = false;
        }

        #endregion

        #region Death Screen

        private GameObject deathScreen;
        private Text deathScreenPlayerName;
        private Text deathScreenInfoText;

        void InitializeDeathScreen()
        {
            deathScreen = transform.Find("Player/GUI/DeathFeed").gameObject;
            deathScreenPlayerName = transform.Find("Player/GUI/DeathFeed/Image/enemy name").GetComponent<Text>();
            deathScreenInfoText = transform.Find("Player/GUI/DeathFeed/Image/Text").GetComponent<Text>();
        }

        public void SetDeathScreen(bool active)
        {
            deathScreen.SetActive(active);
        }

        public void UpdateDeathScreen(bool suicide, string killer)
        {
            if (suicide)
            {
                deathScreenInfoText.text = "YOU COMMITED SUICIDE";
                deathScreenPlayerName.text = "EPIC FAIL!";
            }
            else
            {
                deathScreenInfoText.text = "YOU WHERE KILLED BY";
                deathScreenPlayerName.text = killer;
            }
        }

        IEnumerator HandleDeathScreen()
        {
            SetDeathScreen(true);
            yield return new WaitForSeconds(5);
            SetDeathScreen(false);
        }

        #endregion

        #region Nametags

        List<GameObject> allNametags = new List<GameObject>();

        public void InitializeNametags()
        {
            if (allNametags.Count < MultiplayerManager.matchSettings.serverSettings.maxPlayers)
            {
                if (allNametags.Count != 0)//Destroy and clear array before continueing
                    for (int i = 0; i < allNametags.Count; i++)
                        Destroy(allNametags[i]);
                allNametags.Clear();

                //Create new nametags
                for (int i = 0; i < MultiplayerManager.matchSettings.serverSettings.maxPlayers; i++)
                {
                    GameObject go = Instantiate(elements.nametagObject) as GameObject;
                    go.transform.SetParent(elements.nametagObject.transform.parent, true);
                    go.transform.localScale = Vector3.one;
                    PlayerTag pt = go.GetComponent<PlayerTag>();
                    pt.targetCamera = localPlayer.playerCamera.camera;
                    pt.targetPlayer = MultiplayerManager.GetPlayers()[i];
                    pt.cameraOwner = localPlayer.clientPlayer;
                    pt.Init();
                    allNametags.Add(go);
                }
            }

            for (int i = 0; i < allNametags.Count; i++)
                allNametags[i].GetComponent<PlayerTag>().UpdateNameTag();
        }

        public void UpdateNametag(ClientPlayer player)
        {
            allNametags[player.listIndex].GetComponent<PlayerTag>().UpdateNameTag();
        }

        #endregion

        #region Fullscreen Fade

        public void FullscreenFade(bool enable)
        {
            StopCoroutine("HandleFullscreenFade");
            StartCoroutine(HandleFullscreenFade(enable));
        }

        public AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        IEnumerator HandleFullscreenFade(bool enable)
        {
            fadeBlack.enabled = true;
            float startTime = Time.time;
            fadeBlack.color = new Color(0, 0, 0, enable ? 0 : 1);
            while (Time.time - startTime <= 0.25f)
            {
                yield return new WaitForEndOfFrame();
                fadeBlack.color = Color.Lerp(new Color(0, 0, 0, enable ? 0 : 1), new Color(0, 0, 0, enable ? 1 : 0), curve.Evaluate((Time.time - startTime) * 4));
            }
            fadeBlack.enabled = enable;
            fadeBlack.color = new Color(0, 0, 0, enable ? 1 : 0);
        }

        #endregion
    }
}