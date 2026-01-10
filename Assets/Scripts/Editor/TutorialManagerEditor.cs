using UnityEngine;
using UnityEditor;
using HairRemovalSim.Core;
using System.Collections.Generic;

namespace HairRemovalSim.Editor
{
    /// <summary>
    /// Custom editor for TutorialManager that auto-populates tutorial definitions
    /// </summary>
    [CustomEditor(typeof(TutorialManager))]
    public class TutorialManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Auto-Populate Tutorial Definitions", GUILayout.Height(30)))
            {
                PopulateTutorials();
            }
            
            EditorGUILayout.HelpBox(
                "Click the button above to auto-populate all tutorial definitions.\n" +
                "This will add 5 existing + 22 new tutorials with proper completion actions.", 
                MessageType.Info);
        }
        
        private void PopulateTutorials()
        {
            TutorialManager manager = (TutorialManager)target;
            
            // Access the private tutorials field via serialization
            SerializedProperty tutorialsProperty = serializedObject.FindProperty("tutorials");
            
            // Clear existing
            tutorialsProperty.ClearArray();
            
            // Define all tutorials with CORRECT requiresAction and completeAction based on user's table
            var definitions = new List<TutorialDefinitionData>
            {
                // ====== EXISTING TUTORIALS (from Inspector) ======
                new TutorialDefinitionData(
                    id: "FirstTreatmentShaver",
                    messageKey: "",
                    fallbackMessage: "Qキーで希望部位をハイライト\nまずはシェーバーで希望部位の毛を短く剃ろう",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "treatment_start",
                    requiresAction: true,
                    completeAction: "treatment_start"
                ),
                new TutorialDefinitionData(
                    id: "FirstTreatmentLaser",
                    messageKey: "",
                    fallbackMessage: "毛が短くなったらレーザーで残りの毛を焼き払おう",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "UseLaserEvent",
                    requiresAction: true,
                    completeAction: "treatment_end"
                ),
                new TutorialDefinitionData(
                    id: "FirstTreatmentEnd",
                    messageKey: "",
                    fallbackMessage: "施術が完了しました！\nベッドから出てお客様の会計に移りましょう。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "FirstTreatmentEnd",
                    requiresAction: true,
                    completeAction: "treatment_end"
                ),
                new TutorialDefinitionData(
                    id: "ReceptionFirst",
                    messageKey: "",
                    fallbackMessage: "お客様が希望する部位を選択し\n金額を確定させましょう",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "ReceptionFirst",
                    requiresAction: true,
                    completeAction: "ReceptionFirst"
                ),
                new TutorialDefinitionData(
                    id: "CashierFirst",
                    messageKey: "",
                    fallbackMessage: "金額を確定し代金をいただきましょう。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "CashierFirst",
                    requiresAction: true,
                    completeAction: "CashierFirst"
                ),
                
                // ====== NEW TUTORIALS (from Tutorial.csv) ======
                // Day 1: サロンオープン - 終了条件: ドアに初めてインタラクトしたら
                new TutorialDefinitionData(
                    id: "tut_salon_open",
                    messageKey: "tutorial.salon_open",
                    fallbackMessage: "あなたの脱毛サロンがオープンしました！ドアにインタラクトして営業をスタートさせましょう！",
                    trigger: TutorialTrigger.DayStart,
                    triggerDay: 1,
                    triggerEvent: "",
                    requiresAction: true,
                    completeAction: "door_first_interact"
                ),
                
                // Day 1夜: 掃除 - 終了条件: 掃除完了後
                new TutorialDefinitionData(
                    id: "tut_cleaning",
                    messageKey: "tutorial.cleaning",
                    fallbackMessage: "お客様の毛が床に落ちています。掃除しなければレビューが下がってしまうでしょう。",
                    trigger: TutorialTrigger.DayEnd,
                    triggerDay: 1,
                    triggerEvent: "",
                    requiresAction: true,
                    completeAction: "cleaning_complete"
                ),
                
                // Day 1夜: 営業終了 - 終了条件: 営業終了後、ドアにインタラクトしたら
                new TutorialDefinitionData(
                    id: "tut_day_end",
                    messageKey: "tutorial.day_end",
                    fallbackMessage: "すべてのお客様の施術が終わったらドアから帰ることができます。",
                    trigger: TutorialTrigger.DayEnd,
                    triggerDay: 1,
                    triggerEvent: "",
                    requiresAction: true,
                    completeAction: "door_night_interact"
                ),
                
                // Day 1夜: アイテムショップオープン - 終了条件: StorePanelを開いたとき
                new TutorialDefinitionData(
                    id: "tut_store_open",
                    messageKey: "tutorial.store_open",
                    fallbackMessage: "アイテムショップがオープンしました！パソコンからアイテムを買ってみましょう。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "cleaning_complete",
                    requiresAction: true,
                    completeAction: "store_panel_opened"
                ),
                
                // Day 1夜: アイテムについて - 終了条件: StorePanelを閉じたとき
                new TutorialDefinitionData(
                    id: "tut_item_about",
                    messageKey: "tutorial.item_about",
                    fallbackMessage: "アイテムは受付やレジでお客様にご提案したり、施術中のサポートツールとして使用できます。\n購入したアイテムは翌朝に届きますが、追加料金で即時配達も可能です。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "store_panel_opened",
                    requiresAction: true,
                    completeAction: "store_panel_closed"
                ),
                
                // Day 1夜: レビューチェック - 終了条件: ReviewPanelを閉じたとき
                new TutorialDefinitionData(
                    id: "tut_review_check",
                    messageKey: "tutorial.review_check",
                    fallbackMessage: "お客様がレビューを描きこんでくれました！PCから確認してみましょう。\nレビューをたくさん集めて星レベルをアップさせましょう！",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "store_panel_closed_day1",
                    requiresAction: true,
                    completeAction: "review_panel_closed"
                ),
                
                // any: アイテムの移動 - 終了条件: 15秒経過後
                new TutorialDefinitionData(
                    id: "tut_item_move",
                    messageKey: "tutorial.item_move",
                    fallbackMessage: "倉庫にあるアイテムは施術棚、受付、レジに移動させると使えるようになります。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "warehouse_panel_opened",
                    requiresAction: false, // 15秒後に自動消去なのでfalse
                    completeAction: ""
                ),
                
                // any: 受付アイテム - 終了条件: ReceptionPanelを閉じたとき
                new TutorialDefinitionData(
                    id: "tut_reception_item",
                    messageKey: "tutorial.reception_item",
                    fallbackMessage: "お客様の予算に応じて商品を提案しましょう。\nストックからアイテムをドロップできます。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "reception_has_items",
                    requiresAction: true,
                    completeAction: "reception_panel_closed"
                ),
                
                // any: レジアイテム - 終了条件: PaymentPanelを閉じたとき
                new TutorialDefinitionData(
                    id: "tut_cashier_item",
                    messageKey: "tutorial.cashier_item",
                    fallbackMessage: "お客様の満足度に応じて商品を提案しましょう。\nストックからアイテムをドロップできます。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "payment_has_items",
                    requiresAction: true,
                    completeAction: "payment_panel_closed"
                ),
                
                // any: 痛みゲージ - 終了条件: 15秒経過後
                new TutorialDefinitionData(
                    id: "tut_pain_gauge",
                    messageKey: "tutorial.pain_gauge",
                    fallbackMessage: "痛みゲージが3回MAXになるとお客様が帰ってしまいます。\n冷却ジェルを使ってうまく脱毛しましょう。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "pain_gauge_high",
                    requiresAction: false, // 15秒後に自動消去なのでfalse
                    completeAction: ""
                ),
                
                // any: 顔用レーザー - 終了条件: 10秒後
                new TutorialDefinitionData(
                    id: "tut_face_laser",
                    messageKey: "tutorial.face_laser",
                    fallbackMessage: "顔用レーザーはひげ、脇を脱毛できます。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "face_laser_equipped",
                    requiresAction: false, // 10秒後に自動消去なのでfalse
                    completeAction: ""
                ),
                
                // any: 体用レーザー - 終了条件: 10秒後
                new TutorialDefinitionData(
                    id: "tut_body_laser",
                    messageKey: "tutorial.body_laser",
                    fallbackMessage: "体用レーザーはひげ、脇以外のすべての部位を脱毛できます。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "body_laser_equipped",
                    requiresAction: false, // 10秒後に自動消去なのでfalse
                    completeAction: ""
                ),
                    
                // Day 2朝: 銀行オープン - 終了条件: BankPanelを開いたとき
                new TutorialDefinitionData(
                    id: "tut_bank_open",
                    messageKey: "tutorial.bank_open",
                    fallbackMessage: "銀行から融資を受けられるようになりました！",
                    trigger: TutorialTrigger.DayStart,
                    triggerDay: 2,
                    triggerEvent: "",
                    requiresAction: true,
                    completeAction: "bank_panel_opened"
                ),
                
                // Day 2朝: 広告オープン - 終了条件: AdvertisementPanelを開いたとき
                new TutorialDefinitionData(
                    id: "tut_ad_open",
                    messageKey: "tutorial.ad_open",
                    fallbackMessage: "広告を活用して客数を増やしましょう！",
                    trigger: TutorialTrigger.DayStart,
                    triggerDay: 2,
                    triggerEvent: "",
                    requiresAction: true,
                    completeAction: "ad_panel_opened"
                ),
                
                // Day 2朝: 広告について - 終了条件: AdvertisementPanelを閉じたとき
                new TutorialDefinitionData(
                    id: "tut_ad_about",
                    messageKey: "tutorial.ad_about",
                    fallbackMessage: "広告を打つと集客度が上がり客数が増えます。\n良質な広告になればなるほど来店するお客様のランクも高くなります。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "ad_panel_opened",
                    requiresAction: true,
                    completeAction: "ad_panel_closed"
                ),
                
                // any: 支払いについて - 終了条件: PaymentListPanelを閉じたとき
                new TutorialDefinitionData(
                    id: "tut_payment_about",
                    messageKey: "tutorial.payment_about",
                    fallbackMessage: "家賃、ローンの支払いはこまめにしましょう。支払いが遅れると延滞金が発生したり、ひどいと取り立てが来てサロンを運営できなくなります。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "payment_list_opened",
                    requiresAction: true,
                    completeAction: "payment_list_closed"
                ),
                    
                // ★3: ツールショップオープン - 終了条件: ToolShopPanelを開いたとき
                new TutorialDefinitionData(
                    id: "tut_toolshop_open",
                    messageKey: "tutorial.toolshop_open",
                    fallbackMessage: "ツールショップがオープンしました！新しいレーザーを買いましょう！",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "star_rating_3",
                    requiresAction: true,
                    completeAction: "toolshop_panel_opened"
                ),
                
                // any: ツールショップについて - 終了条件: ToolShopPanelを閉じたとき
                new TutorialDefinitionData(
                    id: "tut_toolshop_about",
                    messageKey: "tutorial.toolshop_about",
                    fallbackMessage: "ツールショップでは新しいレーザーや装飾品、便利アイテムを購入することができます。\nうまく活用してお客様にとって心地よいサロンをつくりましょう",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "toolshop_panel_opened",
                    requiresAction: true,
                    completeAction: "toolshop_panel_closed"
                ),
                
                // ★4: スタッフオープン - 終了条件: StaffPanelを開いたとき
                new TutorialDefinitionData(
                    id: "tut_staff_open",
                    messageKey: "tutorial.staff_open",
                    fallbackMessage: "スタッフを雇用できるようになりました！PCから従業員を雇ってみましょう！",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "star_rating_4",
                    requiresAction: true,
                    completeAction: "staff_panel_opened"
                ),
                
                // any: スタッフについて - 終了条件: StaffPanelを閉じたとき
                new TutorialDefinitionData(
                    id: "tut_staff_about",
                    messageKey: "tutorial.staff_about",
                    fallbackMessage: "スタッフを雇用すると受付、レジ、施術、在庫補充を代行してくれます。ランクに応じて仕事の効率や失敗率が変わります。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "staff_panel_opened",
                    requiresAction: true,
                    completeAction: "staff_panel_closed"
                ),
                
                // ★5: 店舗拡大オープン - 終了条件: 15秒
                new TutorialDefinitionData(
                    id: "tut_upgrade_open",
                    messageKey: "tutorial.upgrade_open",
                    fallbackMessage: "店舗を拡大できるようになりました！PCからアップグレードできます。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "star_rating_5",
                    requiresAction: false, // 15秒後に自動消去
                    completeAction: ""
                ),
                
                // any: 店舗拡大について - 終了条件: ShopUpgradePanelを閉じたとき
                new TutorialDefinitionData(
                    id: "tut_upgrade_about",
                    messageKey: "tutorial.upgrade_about",
                    fallbackMessage: "店舗を拡大すると、ベッドの数が増えたり最大集客度が増えたりします。\n家賃も高くなりますが、その分稼げばいいんです。",
                    trigger: TutorialTrigger.Event,
                    triggerDay: 0,
                    triggerEvent: "upgrade_panel_opened",
                    requiresAction: true,
                    completeAction: "upgrade_panel_closed"
                ),
            };
            
            // Add each definition
            for (int i = 0; i < definitions.Count; i++)
            {
                tutorialsProperty.InsertArrayElementAtIndex(i);
                SerializedProperty element = tutorialsProperty.GetArrayElementAtIndex(i);
                
                var def = definitions[i];
                element.FindPropertyRelative("id").stringValue = def.id;
                element.FindPropertyRelative("messageKey").stringValue = def.messageKey;
                element.FindPropertyRelative("fallbackMessage").stringValue = def.fallbackMessage;
                element.FindPropertyRelative("trigger").enumValueIndex = (int)def.trigger;
                element.FindPropertyRelative("triggerDay").intValue = def.triggerDay;
                element.FindPropertyRelative("triggerEvent").stringValue = def.triggerEvent;
                element.FindPropertyRelative("requiresAction").boolValue = def.requiresAction;
                element.FindPropertyRelative("completeAction").stringValue = def.completeAction;
            }
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            
            Debug.Log($"[TutorialManagerEditor] Populated {definitions.Count} tutorial definitions with proper RequiresAction and CompleteAction");
        }
        
        // Helper struct for tutorial data
        private struct TutorialDefinitionData
        {
            public string id;
            public string messageKey;
            public string fallbackMessage;
            public TutorialTrigger trigger;
            public int triggerDay;
            public string triggerEvent;
            public bool requiresAction;
            public string completeAction;
            
            public TutorialDefinitionData(string id, string messageKey, string fallbackMessage,
                TutorialTrigger trigger, int triggerDay, string triggerEvent, bool requiresAction, string completeAction)
            {
                this.id = id;
                this.messageKey = messageKey;
                this.fallbackMessage = fallbackMessage;
                this.trigger = trigger;
                this.triggerDay = triggerDay;
                this.triggerEvent = triggerEvent;
                this.requiresAction = requiresAction;
                this.completeAction = completeAction;
            }
        }
    }
}
