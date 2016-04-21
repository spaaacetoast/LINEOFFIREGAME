using UnityEngine;
using System.Collections;
using TNet;

namespace AngryRain.Multiplayer.LevelEditor
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SyncNetworkObject))]
    public class LevelObjectManager : TNBehaviour
    {
        public bool isStaticObject; //Is the object in the scene before the custommap is loaded
        public bool isSelected; //Is this object selected in the level editor
        public bool editTransform; //Should we change the transform according to the player, Is the player holding the object in editing mode
        public bool isLevelObject = true; //Is this object a level object or something different e.g. animated spawn droppod
        public int respawnTime = 30; //Respawn timer for destroying or instancing this object
        public Vector3 transformOffset;

        public ClientPlayer thisOwner { get; set; }
        public LevelManager.LevelObject thisLevelObject { get; set; }
        public int objectID { get; set; }

        public int objectIndex { get; set; } //The object index number from the global list
        public int instanceID { get; set; }

        public ObjectState currentObjectState;
        public ObjectState runtimeObjectState = ObjectState.Normal; //Object state when this object is not being selected or used somewhere
        public bool canRuntimeObjectStateBeChanged;
        public bool keepActiveWhenSleeping; //Wont activate any optimizations that may run when rigidbody is going to sleep for interactive objects

        public Vector3 startPosition { get; set; }
        public Quaternion startRotation { get; set; }

        public new Rigidbody rigidbody { private set; get; }
        public SyncNetworkObject multiplayerObject { private set; get; }

        void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
            multiplayerObject = GetComponent<SyncNetworkObject>();

            DontDestroyOnLoad(gameObject);
        }

        IEnumerator Start()
        {
            yield return null;

            if (LevelManager.instance != null && isLevelObject)
            {
                thisLevelObject = LevelManager.instance.allLevelObjects[objectIndex];

                if (isLevelObject)
                    LevelManager.allSceneLevelObjectManagers.Add(this);
                else
                    LevelManager.allLevelObjectManagers.Add(this);
            }
        }

        void OnDestroy()
        {
            if (thisOwner != null)
            {
                if (thisOwner.currentHoldingObject = this) thisOwner.currentHoldingObject = null;

                LevelManager.allLevelObjectManagers.Remove(this);
            }
        }

        [RFC(20)]
        public void ServerSetSelect(bool shouldSelect, int player, bool resetTransform)
        {
            if (TNManager.isHosting)
            {
                //MPlayer mPlayer = MultiplayerManager.GetPlayer(player);

                if (!shouldSelect)
                {
                    ServerSetOwner(-1, true);
                    tno.Send(24, Target.All, false, resetTransform);
                }
                else if (!isSelected)
                {
                    ServerSetOwner(player, false);
                    tno.Send(24, Target.All, shouldSelect, resetTransform);
                }
            }
            else
                tno.Send(20, Target.Host, shouldSelect, player, resetTransform);
        }

        [RFC(24)]
        public void ClientSetSelect(bool shouldSelect, bool resetTransform)
        {
            isSelected = shouldSelect;
            thisOwner.currentHoldingObject = shouldSelect ? this : null;
            if (resetTransform)
            {
                rigidbody.position = startPosition;
                rigidbody.rotation = startRotation;
            }
        }

        /// <summary>
        /// isMoving - Is this object being transformed by the player
        /// </summary>
        /// <param name="o"></param>
        /// <param name="isMoving"></param>
        [RFC(21)]
        public void ServerSetObjectState(byte o, bool isMoving)
        {
            if (TNManager.isHosting)
                tno.Send(25, Target.All, o, isMoving);
            else
                tno.Send(21, Target.Host, o, isMoving);
        }

        [RFC(25)]
        public void ClientSetObjectState(byte o, bool isMoving)
        {
            currentObjectState = (ObjectState)o;

            if (currentObjectState == ObjectState.Normal)
            {
                rigidbody.isKinematic = false;
                rigidbody.useGravity = !isMoving;
                rigidbody.detectCollisions = !isMoving;
            }
            else if (currentObjectState == ObjectState.Fixed)
            {
                rigidbody.isKinematic = !isMoving;
                rigidbody.useGravity = false;
                rigidbody.detectCollisions = !isMoving;
            }
            else if (currentObjectState == ObjectState.Phased)
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                rigidbody.detectCollisions = !isMoving;
            }

            if (isMoving)
            {
                rigidbody.position = startPosition;
            }
        }

        [RFC(23)]
        public void ServerSetOwner(int player, bool resetState)
        {
            if (TNManager.isHosting)
            {
                tno.Send(26, Target.All, player, resetState);
            }
            else
                tno.Send(23, Target.Host, player, resetState);
        }

        [RFC(26)]
        public void ClientSetOwner(int player, bool resetState)
        {
            ClientPlayer mPlayer = MultiplayerManager.GetPlayer(player);

            //Any old player needs resetting

            if (thisOwner != null && thisOwner != mPlayer)
            {
                if (thisOwner.currentHoldingObject == this) thisOwner.currentHoldingObject = null;
            }

            //When reset is wanted, reset object and player

            if (resetState)
            {
                if (TNManager.isHosting)
                {
                    ServerSetObjectState((byte)runtimeObjectState, false);
                }
            }

            //Assign new player to ownership

            //tno.ownerID = mPlayer.tPlayer.id;
            thisOwner = mPlayer;

            //syncRigidbody.SetRole(mPlayer.isHost ? FlowType.ServerToClient : FlowType.ClientToServer);
        }

        bool isHoldingThis(ClientPlayer player)
        {
            return player != null && player.currentHoldingObject == this;
        }

        public void Local_SyncSaveParameters()
        {
            startPosition = rigidbody.position;
            startRotation = rigidbody.rotation;

            tno.Send(27, Target.Others, Serializer.Serialize<SyncContainer>( new SyncContainer() {
                startPosition = SyncContainer.GetSerializableVector3(this.startPosition),
                startRotation = SyncContainer.GetSerializableQuaternion(this.startRotation)
            }));
        }

        [RFC(27)]
        void Client_ReceiveSyncSaveParameters(byte[] parameters)
        {
            SyncContainer syncContainer = Serializer.Deserialize<SyncContainer>(parameters);

            startPosition = SyncContainer.GetVector3(syncContainer.startPosition);
            startRotation = SyncContainer.GetQuaternion(syncContainer.startRotation);
        }

        [System.Serializable]
        class SyncContainer
        {
            public SVector3 startPosition;
            public SQuaternion startRotation;

            #region SVector3

            public static SVector3 GetSerializableVector3(Vector3 vec)
            {
                return new SVector3() { x = vec.x, y = vec.y, z = vec.z };
            }

            public static Vector3 GetVector3(SVector3 vec)
            {
                return new Vector3() { x = vec.x, y = vec.y, z = vec.z };
            }

            [System.Serializable]
            public struct SVector3
            {
                public float x, y, z;
            }

            #endregion

            #region SQuaternion

            public static SQuaternion GetSerializableQuaternion(Quaternion vec)
            {
                return new SQuaternion() { x = vec.x, y = vec.y, z = vec.z, w = vec.w };
            }

            public static Quaternion GetQuaternion(SQuaternion vec)
            {
                return new Quaternion() { x = vec.x, y = vec.y, z = vec.z, w = vec.w };
            }

            [System.Serializable]
            public struct SQuaternion
            {
                public float x, y, z, w;
            }

            #endregion
        }
    }

    public enum ObjectState
    {
        Normal,
        Fixed,
        Phased
    }
}