using UnityEngine;
using System.Collections.Generic;
using HairRemovalSim.UI;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Tutorial trigger types
    /// </summary>
    public enum TutorialTrigger
    {
        Event,      // Triggered by specific game event
        DayStart,   // Triggered when day starts
        DayEnd      // Triggered when day ends
    }
    
    /// <summary>
    /// Tutorial definition
    /// </summary>
    [System.Serializable]
    public class TutorialDefinition
    {
        public string id;
        public string messageKey;       // Localization key (e.g., "tutorial_first_open")
        public string fallbackMessage;  // Fallback if localization fails (Japanese)
        public TutorialTrigger trigger;
        public int triggerDay;          // For DayStart/DayEnd triggers (0 = any day)
        public string triggerEvent;     // For Event triggers
        public bool requiresAction;     // If true, must call CompleteByAction to mark complete
        public string completeAction;   // Action ID that completes this tutorial
    }
    
    /// <summary>
    /// Manages tutorial messages displayed via MessageBoxManager
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }
        
        /// <summary>
        /// True while a tutorial is being shown (for sound conflict prevention)
        /// </summary>
        public bool IsShowingTutorial { get; private set; }
        private Coroutine tutorialSoundCooldown;
        
        [Header("Tutorial Definitions")]
        [SerializeField] private List<TutorialDefinition> tutorials = new List<TutorialDefinition>();
        
        // Completed tutorial IDs (saved/loaded with SaveManager)
        private HashSet<string> completedTutorials = new HashSet<string>();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeDefaultTutorials();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Subscribe to day events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDayStarted += OnDayStart;
                GameManager.Instance.OnDayEnded += OnDayEnd;
            }
        }
        
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDayStarted -= OnDayStart;
                GameManager.Instance.OnDayEnded -= OnDayEnd;
            }
        }
        
        /// <summary>
        /// Initialize default tutorials (only if Inspector is empty)
        /// </summary>
        private void InitializeDefaultTutorials()
        {
            // Only use code-defined tutorials if Inspector is empty
            if (tutorials.Count > 0) return;
            
            tutorials = new List<TutorialDefinition>
            {
                // Day 1 tutorials
                new TutorialDefinition
                {
                    id = "tut_salon_open",
                    messageKey = "tutorial.salon_open",
                    fallbackMessage = "あなたの脱毛サロンがオープンしました！ドアにインタラクトして営業をスタートさせましょう！",
                    trigger = TutorialTrigger.DayStart,
                    triggerDay = 1
                },
                new TutorialDefinition
                {
                    id = "tut_cleaning",
                    messageKey = "tutorial.cleaning",
                    fallbackMessage = "お客様の毛が床に落ちています。掃除しなければレビューが下がってしまうでしょう。",
                    trigger = TutorialTrigger.DayEnd,
                    triggerDay = 1
                },
                new TutorialDefinition
                {
                    id = "tut_day_end",
                    messageKey = "tutorial.day_end",
                    fallbackMessage = "すべてのお客様の施術が終わったらドアから帰ることができます。",
                    trigger = TutorialTrigger.DayEnd,
                    triggerDay = 1
                },
                new TutorialDefinition
                {
                    id = "tut_store_open",
                    messageKey = "tutorial.store_open",
                    fallbackMessage = "アイテムショップがオープンしました！パソコンからアイテムを買ってみましょう。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "cleaning_complete"
                },
                new TutorialDefinition
                {
                    id = "tut_item_about",
                    messageKey = "tutorial.item_about",
                    fallbackMessage = "アイテムは受付やレジでお客様に提案したり、施術中のサポートツールとして使用できます。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "store_panel_opened"
                },
                new TutorialDefinition
                {
                    id = "tut_review_check",
                    messageKey = "tutorial.review_check",
                    fallbackMessage = "お客様がレビューを書いてくれました！PCから確認してみましょう。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "store_panel_closed_day1"
                },
                
                // Day 2 tutorials
                new TutorialDefinition
                {
                    id = "tut_bank_open",
                    messageKey = "tutorial.bank_open",
                    fallbackMessage = "銀行から融資を受けられるようになりました！",
                    trigger = TutorialTrigger.DayStart,
                    triggerDay = 2
                },
                new TutorialDefinition
                {
                    id = "tut_ad_open",
                    messageKey = "tutorial.ad_open",
                    fallbackMessage = "広告を活用して客数を増やしましょう！",
                    trigger = TutorialTrigger.DayStart,
                    triggerDay = 2
                },
                new TutorialDefinition
                {
                    id = "tut_ad_about",
                    messageKey = "tutorial.ad_about",
                    fallbackMessage = "広告を打つと集客度が上がり客数が増えます。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "ad_panel_opened"
                },
                
                // Any day tutorials
                new TutorialDefinition
                {
                    id = "tut_item_move",
                    messageKey = "tutorial.item_move",
                    fallbackMessage = "倉庫にあるアイテムは施術棚、受付、レジに移動させると使えるようになります。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "warehouse_panel_opened"
                },
                new TutorialDefinition
                {
                    id = "tut_reception_item",
                    messageKey = "tutorial.reception_item",
                    fallbackMessage = "お客様の予算に応じて商品を提案しましょう。ストックからアイテムをドロップできます。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "reception_has_items"
                },
                new TutorialDefinition
                {
                    id = "tut_cashier_item",
                    messageKey = "tutorial.cashier_item",
                    fallbackMessage = "お客様の満足度に応じて商品を提案しましょう。ストックからアイテムをドロップできます。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "payment_has_items"
                },
                new TutorialDefinition
                {
                    id = "tut_pain_gauge",
                    messageKey = "tutorial.pain_gauge",
                    fallbackMessage = "痛みゲージが3回MAXになるとお客様が帰ってしまいます。冷却ジェルを使ってうまく脱毛しましょう。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "pain_gauge_high"
                },
                new TutorialDefinition
                {
                    id = "tut_face_laser",
                    messageKey = "tutorial.face_laser",
                    fallbackMessage = "顔用レーザーはひげ、脇を脱毛できます。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "face_laser_equipped"
                },
                new TutorialDefinition
                {
                    id = "tut_body_laser",
                    messageKey = "tutorial.body_laser",
                    fallbackMessage = "体用レーザーはひげ、脇以外のすべての部位を脱毛できます。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "body_laser_equipped"
                },
                new TutorialDefinition
                {
                    id = "tut_payment_about",
                    messageKey = "tutorial.payment_about",
                    fallbackMessage = "家賃、ローンの支払いはこまめにしましょう。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "payment_list_opened"
                },
                
                // Star rating triggered tutorials
                new TutorialDefinition
                {
                    id = "tut_toolshop_open",
                    messageKey = "tutorial.toolshop_open",
                    fallbackMessage = "ツールショップがオープンしました！新しいレーザーを買いましょう！",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "star_rating_3"
                },
                new TutorialDefinition
                {
                    id = "tut_toolshop_about",
                    messageKey = "tutorial.toolshop_about",
                    fallbackMessage = "ツールショップでは新しいレーザーや装飾品、便利アイテムを購入できます。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "toolshop_panel_opened"
                },
                new TutorialDefinition
                {
                    id = "tut_staff_open",
                    messageKey = "tutorial.staff_open",
                    fallbackMessage = "スタッフを雇用できるようになりました！PCから従業員を雇ってみましょう！",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "star_rating_4"
                },
                new TutorialDefinition
                {
                    id = "tut_staff_about",
                    messageKey = "tutorial.staff_about",
                    fallbackMessage = "スタッフを雇用すると受付、レジ、施術、在庫補充を代行してくれます。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "staff_panel_opened"
                },
                new TutorialDefinition
                {
                    id = "tut_upgrade_open",
                    messageKey = "tutorial.upgrade_open",
                    fallbackMessage = "店舗を拡大できるようになりました！PCからアップグレードできます。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "star_rating_5"
                },
                new TutorialDefinition
                {
                    id = "tut_upgrade_about",
                    messageKey = "tutorial.upgrade_about",
                    fallbackMessage = "店舗を拡大するとベッド数が増え、最大集客度も増えます。",
                    trigger = TutorialTrigger.Event,
                    triggerEvent = "upgrade_panel_opened"
                }
            };
        }
        
        /// <summary>
        /// Try to show a tutorial by ID (only if not already completed)
        /// </summary>
        public bool TryShowTutorial(string tutorialId)
        {
            if (string.IsNullOrEmpty(tutorialId)) return false;
            if (completedTutorials.Contains(tutorialId)) return false;
            
            var tutorial = tutorials.Find(t => t.id == tutorialId);
            if (tutorial == null) return false;
            
            ShowTutorialMessage(tutorial);
            
            // Mark as completed if no action required
            if (!tutorial.requiresAction)
            {
                completedTutorials.Add(tutorialId);
            }
            
            return true;
        }
        
        /// <summary>
        /// Trigger tutorials by event name
        /// </summary>
        public void TriggerEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            
            foreach (var tutorial in tutorials)
            {
                if (tutorial.trigger == TutorialTrigger.Event && 
                    tutorial.triggerEvent == eventName)
                {
                    TryShowTutorial(tutorial.id);
                }
            }
        }
        
        /// <summary>
        /// Complete a tutorial by action ID
        /// </summary>
        public void CompleteByAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return;
            
            foreach (var tutorial in tutorials)
            {
                if (tutorial.requiresAction && tutorial.completeAction == actionId)
                {
                    completedTutorials.Add(tutorial.id);
                    
                    // Dismiss the message from MessageBox
                    MessageBoxManager.Instance?.DismissMessage(tutorial.id);
                    
                    Debug.Log($"[TutorialManager] Tutorial '{tutorial.id}' completed by action '{actionId}'");
                }
            }
        }
        
        /// <summary>
        /// Manually mark a tutorial as completed
        /// </summary>
        public void CompleteTutorial(string tutorialId)
        {
            if (!string.IsNullOrEmpty(tutorialId))
            {
                completedTutorials.Add(tutorialId);
                MessageBoxManager.Instance?.DismissMessage(tutorialId);

            }
        }
        
        /// <summary>
        /// Check if a tutorial is completed
        /// </summary>
        public bool IsTutorialCompleted(string tutorialId)
        {
            return completedTutorials.Contains(tutorialId);
        }
        
        /// <summary>
        /// Get list of completed tutorials (for saving)
        /// </summary>
        public List<string> GetCompletedTutorials()
        {
            return new List<string>(completedTutorials);
        }
        
        /// <summary>
        /// Set completed tutorials (for loading)
        /// </summary>
        public void SetCompletedTutorials(List<string> completed)
        {
            completedTutorials.Clear();
            if (completed != null)
            {
                foreach (var id in completed)
                {
                    completedTutorials.Add(id);
                }
            }
        }
        
        /// <summary>
        /// Reset all tutorials (for new game)
        /// </summary>
        public void ResetAllTutorials()
        {
            completedTutorials.Clear();
            Debug.Log("[TutorialManager] All tutorials reset");
        }
        
        private void OnDayStart(int day)
        {
            foreach (var tutorial in tutorials)
            {
                if (tutorial.trigger == TutorialTrigger.DayStart)
                {
                    if (tutorial.triggerDay == 0 || tutorial.triggerDay == day)
                    {
                        TryShowTutorial(tutorial.id);
                    }
                }
            }
        }
        
        private void OnDayEnd(int day)
        {
            foreach (var tutorial in tutorials)
            {
                if (tutorial.trigger == TutorialTrigger.DayEnd)
                {
                    if (tutorial.triggerDay == 0 || tutorial.triggerDay == day)
                    {
                        TryShowTutorial(tutorial.id);
                    }
                }
            }
        }
        
        private void ShowTutorialMessage(TutorialDefinition tutorial)
        {
            if (MessageBoxManager.Instance == null) return;
            
            // Set flag for sound conflict prevention
            IsShowingTutorial = true;
            if (tutorialSoundCooldown != null)
            {
                StopCoroutine(tutorialSoundCooldown);
            }
            tutorialSoundCooldown = StartCoroutine(ResetTutorialSoundFlag());
            
            // Get localized message, fallback to direct message
            string message = tutorial.fallbackMessage;
            if (!string.IsNullOrEmpty(tutorial.messageKey) && LocalizationManager.Instance != null)
            {
                string localized = LocalizationManager.Instance.Get(tutorial.messageKey);
                // Use localized if not returning the key placeholder
                if (!localized.StartsWith("[") || !localized.EndsWith("]"))
                {
                    message = localized;
                }
            }
            
            var data = new MessageData(tutorial.id, null, MessageType.Tutorial, MessageCategory.Tutorial);
            data.directMessage = message;
            data.playSound = true;
            data.persistent = tutorial.requiresAction; // Stay visible if action required
            
            MessageBoxManager.Instance.ShowMessage(data);
            Debug.Log($"[TutorialManager] Showing tutorial: {tutorial.id}");
        }
        
        private System.Collections.IEnumerator ResetTutorialSoundFlag()
        {
            yield return new WaitForSeconds(0.5f);
            IsShowingTutorial = false;
            tutorialSoundCooldown = null;
        }
    }
}
