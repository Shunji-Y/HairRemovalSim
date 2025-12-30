# Hair Removal Salon Simulator
# ゲームバランス・収益シミュレーター

import random
from dataclasses import dataclass, field
from typing import List, Dict, Tuple
from enum import Enum
import math

# ============================================
# 定数定義
# ============================================

class WealthLevel(Enum):
    POOREST = 0   # 極貧
    POOR = 1      # 貧乏
    NORMAL = 2    # 普通
    RICH = 3      # 富裕
    RICHEST = 4   # 大富豪

class StaffRank(Enum):
    STUDENT = 0       # 大学生
    NEWBIE = 1        # 新卒社員
    REGULAR = 2       # 中堅社員
    VETERAN = 3       # ベテラン
    PRO = 4           # プロ

# スタッフ成功率 (顧客ランク × スタッフランク)
STAFF_SUCCESS_RATE = {
    # (WealthLevel, StaffRank): success_rate
    (WealthLevel.POOREST, StaffRank.STUDENT): 1.00,
    (WealthLevel.POOREST, StaffRank.NEWBIE): 1.00,
    (WealthLevel.POOREST, StaffRank.REGULAR): 1.00,
    (WealthLevel.POOREST, StaffRank.VETERAN): 1.00,
    (WealthLevel.POOREST, StaffRank.PRO): 1.00,
    
    (WealthLevel.POOR, StaffRank.STUDENT): 0.80,
    (WealthLevel.POOR, StaffRank.NEWBIE): 1.00,
    (WealthLevel.POOR, StaffRank.REGULAR): 1.00,
    (WealthLevel.POOR, StaffRank.VETERAN): 1.00,
    (WealthLevel.POOR, StaffRank.PRO): 1.00,
    
    (WealthLevel.NORMAL, StaffRank.STUDENT): 0.60,
    (WealthLevel.NORMAL, StaffRank.NEWBIE): 0.80,
    (WealthLevel.NORMAL, StaffRank.REGULAR): 1.00,
    (WealthLevel.NORMAL, StaffRank.VETERAN): 1.00,
    (WealthLevel.NORMAL, StaffRank.PRO): 1.00,
    
    (WealthLevel.RICH, StaffRank.STUDENT): 0.40,
    (WealthLevel.RICH, StaffRank.NEWBIE): 0.60,
    (WealthLevel.RICH, StaffRank.REGULAR): 0.80,
    (WealthLevel.RICH, StaffRank.VETERAN): 1.00,
    (WealthLevel.RICH, StaffRank.PRO): 1.00,
    
    (WealthLevel.RICHEST, StaffRank.STUDENT): 0.20,
    (WealthLevel.RICHEST, StaffRank.NEWBIE): 0.40,
    (WealthLevel.RICHEST, StaffRank.REGULAR): 0.60,
    (WealthLevel.RICHEST, StaffRank.VETERAN): 0.80,
    (WealthLevel.RICHEST, StaffRank.PRO): 1.00,
}

# スタッフレビュー係数
STAFF_REVIEW_MULTIPLIER = {
    StaffRank.STUDENT: 0.90,
    StaffRank.NEWBIE: 0.95,
    StaffRank.REGULAR: 1.00,
    StaffRank.VETERAN: 1.05,
    StaffRank.PRO: 1.10,
}

# スタッフ日給
STAFF_DAILY_SALARY = {
    StaffRank.STUDENT: 80,
    StaffRank.NEWBIE: 100,
    StaffRank.REGULAR: 150,
    StaffRank.VETERAN: 200,
    StaffRank.PRO: 300,
}


# 顧客分布 (星×レビュースコア) → 各富裕層の割合
# レビュースコア区間: 0-20, 20-50, 50-80, 80-100
CUSTOMER_DISTRIBUTION = {
    1: {  # ★1
        0: {WealthLevel.POOREST: 1.0},
        20: {WealthLevel.POOREST: 0.50, WealthLevel.POOR: 0.50},
        50: {WealthLevel.POOREST: 0.33, WealthLevel.POOR: 0.33, WealthLevel.NORMAL: 0.33},
        80: {WealthLevel.POOREST: 0.25},
        100: {WealthLevel.POOREST: 0.25},
    },
    2: {  # ★2
        0: {WealthLevel.POOREST: 1.0},
        20: {WealthLevel.POOREST: 0.40, WealthLevel.POOR: 0.60},
        50: {WealthLevel.POOREST: 0.30, WealthLevel.POOR: 0.70},
        80: {WealthLevel.POOREST: 0.20, WealthLevel.POOR: 0.80},
        100: {WealthLevel.POOREST: 0.20, WealthLevel.POOR: 0.80},
    },
    3: {  # ★3
        0: {WealthLevel.POOREST: 0.25, WealthLevel.POOR: 0.33, WealthLevel.NORMAL: 0.42},
        20: {WealthLevel.POOREST: 0.20, WealthLevel.POOR: 0.35, WealthLevel.NORMAL: 0.45},
        50: {WealthLevel.POOREST: 0.15, WealthLevel.POOR: 0.25, WealthLevel.NORMAL: 0.60},
        80: {WealthLevel.POOREST: 0.10, WealthLevel.POOR: 0.20, WealthLevel.NORMAL: 0.70},
        100: {WealthLevel.POOREST: 0.10, WealthLevel.POOR: 0.10, WealthLevel.NORMAL: 0.80},
    },
    4: {  # ★4
        0: {WealthLevel.POOREST: 0.10, WealthLevel.POOR: 0.15, WealthLevel.NORMAL: 0.25, WealthLevel.RICH: 0.50},
        20: {WealthLevel.POOREST: 0.05, WealthLevel.POOR: 0.10, WealthLevel.NORMAL: 0.35, WealthLevel.RICH: 0.50},
        50: {WealthLevel.POOREST: 0.05, WealthLevel.POOR: 0.10, WealthLevel.NORMAL: 0.35, WealthLevel.RICH: 0.50},
        80: {WealthLevel.POOREST: 0.05, WealthLevel.POOR: 0.05, WealthLevel.NORMAL: 0.40, WealthLevel.RICH: 0.50},
        100: {WealthLevel.POOREST: 0.0, WealthLevel.POOR: 0.0, WealthLevel.NORMAL: 0.50, WealthLevel.RICH: 0.50},
    },
    5: {  # ★5
        0: {WealthLevel.POOREST: 0.05, WealthLevel.POOR: 0.05, WealthLevel.NORMAL: 0.30, WealthLevel.RICH: 0.40, WealthLevel.RICHEST: 0.20},
        20: {WealthLevel.POOREST: 0.05, WealthLevel.POOR: 0.05, WealthLevel.NORMAL: 0.20, WealthLevel.RICH: 0.50, WealthLevel.RICHEST: 0.20},
        50: {WealthLevel.NORMAL: 0.15, WealthLevel.RICH: 0.45, WealthLevel.RICHEST: 0.40},
        80: {WealthLevel.NORMAL: 0.10, WealthLevel.RICH: 0.40, WealthLevel.RICHEST: 0.50},
        100: {WealthLevel.RICH: 0.20, WealthLevel.RICHEST: 0.80},
    },
    6: {  # ★6
        0: {WealthLevel.NORMAL: 0.15, WealthLevel.RICH: 0.45, WealthLevel.RICHEST: 0.40},
        20: {WealthLevel.NORMAL: 0.10, WealthLevel.RICH: 0.45, WealthLevel.RICHEST: 0.45},
        50: {WealthLevel.RICH: 0.40, WealthLevel.RICHEST: 0.60},
        80: {WealthLevel.RICH: 0.30, WealthLevel.RICHEST: 0.70},
        100: {WealthLevel.RICH: 0.20, WealthLevel.RICHEST: 0.80},
    },
    7: {  # ★7
        0: {WealthLevel.RICH: 0.30, WealthLevel.RICHEST: 0.70},
        20: {WealthLevel.RICH: 0.25, WealthLevel.RICHEST: 0.75},
        50: {WealthLevel.RICH: 0.20, WealthLevel.RICHEST: 0.80},
        80: {WealthLevel.RICH: 0.15, WealthLevel.RICHEST: 0.85},
        100: {WealthLevel.RICHEST: 1.0},
    },
}

# プラン料金 (富裕層別)
PLAN_PRICES = {
    WealthLevel.POOREST: {"chest": 20, "abs": 30, "armpits": 40},
    WealthLevel.POOR: {"back": 60, "beard": 70, "armpits": 45},
    WealthLevel.NORMAL: {"arms": 100, "legs": 120, "beard_hq": 150},
    WealthLevel.RICH: {"arms_legs": 180, "upper_no_arms": 200, "upper": 240},
    WealthLevel.RICHEST: {"full_no_beard": 350, "full_with_beard": 420},
}

# グレード設定 - 目標: ローンあり35日, ローンなし50日
# attraction_cap: 集客度上限, max_customers: 基礎集客数上限(ツール補正前)
# beds: ベッド数, staff_slots: ベッド数+3, rent: 家賃(3日毎)
# 基礎処理能力: 1ベッドあたり18人/日 (低めに設定してローンなしを遅くする)
GRADE_CONFIG = {
    1: {"upgrade_cost": 0, "beds": 1, "staff_slots": 4, "rent": 50, "required_stars": 1, "attraction_cap": 100, "max_customers": 18},
    2: {"upgrade_cost": 6000, "beds": 2, "staff_slots": 5, "rent": 150, "required_stars": 2, "attraction_cap": 250, "max_customers": 36},
    3: {"upgrade_cost": 12000, "beds": 4, "staff_slots": 7, "rent": 400, "required_stars": 3, "attraction_cap": 400, "max_customers": 72},
    4: {"upgrade_cost": 80000, "beds": 6, "staff_slots": 9, "rent": 800, "required_stars": 4, "attraction_cap": 600, "max_customers": 108},
    5: {"upgrade_cost": 350000, "beds": 8, "staff_slots": 11, "rent": 1200, "required_stars": 5, "attraction_cap": 800, "max_customers": 144},
    6: {"upgrade_cost": 1000000, "beds": 10, "staff_slots": 13, "rent": 1800, "required_stars": 6, "attraction_cap": 1200, "max_customers": 220},
    7: {"upgrade_cost": 4500000, "beds": 10, "staff_slots": 13, "rent": 2500, "required_stars": 7, "attraction_cap": 1200, "max_customers": 220},
}
MAX_GRADE = 7

# レビュー閾値 (累積レビュー値 → 星)
# 目標滞在日数に合わせて調整: お金が貯まるタイミングで★がギリギリ足りる
REVIEW_THRESHOLDS = {
    1: 0,
    2: 1200,    # G1滞在短縮
    3: 4500,    # G2滞在短縮
    4: 22000,   # G3滞在短縮
    5: 50000,   # G4滞在短縮
    6: 140000,  # G5滞在短縮
    7: 400000,  # G6滞在短縮
}


# 広告設定 (グレード別解放)
# attraction_boost: 集客度上昇ポイント, duration: 効果日数, decay: 日次減衰ポイント
# cost: コスト, grade_req: 必要グレード

AD_CONFIG = {
    "none": {"cost": 0, "attraction_boost": 0, "duration": 0, "decay": 0, "grade_req": 1},
    "free_sns": {"cost": 0, "attraction_boost": 5, "duration": 3, "grade_req": 1, "decay": 1},
    "flyer": {"cost": 500, "attraction_boost": 15, "duration": 5, "grade_req": 2, "decay": 3},
    "paid_sns": {"cost": 1500, "attraction_boost": 30, "duration": 4, "grade_req": 3, "decay": 5},
    "magazine": {"cost": 3500, "attraction_boost": 50, "duration": 5, "grade_req": 3, "decay": 8},
    "train_poster": {"cost": 6000, "attraction_boost": 80, "duration": 5, "grade_req": 4, "decay": 12},
    "tv_cm": {"cost": 30000, "attraction_boost": 150, "duration": 5, "grade_req": 5, "decay": 25},
    "billboard": {"cost": 60000, "attraction_boost": 200, "duration": 5, "grade_req": 5, "decay": 30},
    "video_ad": {"cost": 20000, "attraction_boost": 100, "duration": 4, "grade_req": 4, "decay": 20},
    "influencer": {"cost": 80000, "attraction_boost": 250, "duration": 5, "grade_req": 6, "decay": 40}, # 瞬間風速すごい
}

# 集客率の日次変動 (ランダム要素)
# 集客率の日次変動 (ランダム要素)
# CUSTOMER_GAUGE_DAILY_VARIANCE = 0.08  # +-8%の変動 (未使用: random.randint(-3, 3)が使用されています)

# アップセル設定
# アップセル設定
# UPSELL_CONFIG = {
#     "after_care_lotion": {"price": 30, "rate": 0.15, "review_bonus": 2},
#     "cosmetic_set": {"price": 50, "rate": 0.10, "review_bonus": 3},
#     "premium_lotion": {"price": 80, "rate": 0.08, "review_bonus": 5},
#     "premium_set": {"price": 150, "rate": 0.05, "review_bonus": 8},
# }

# 施術ツール設定 (グレード別) - コスト調整済み
# type: ツール種類 (face/body/shaver), cost: 購入コスト(1台), time_reduction: 施術時間短縮率, review_bonus: レビューボーナス
TOOL_CONFIG = {
    1: {
        "face": {"name": "スムースレイフェイス", "cost": 500, "time_reduction": 0.0, "review_bonus": 0},
        "body": {"name": "スムースレイボディ", "cost": 500, "time_reduction": 0.0, "review_bonus": 0},
        "shaver": {"name": "おんぼろシェーバー", "cost": 250, "time_reduction": 0.0, "review_bonus": 0},
    },
    2: {
        "face": {"name": "スムースレイフェイスプロ", "cost": 1000, "time_reduction": 0.10, "review_bonus": 2},
        "body": {"name": "スムースレイボディプロ", "cost": 1000, "time_reduction": 0.10, "review_bonus": 2},
        "shaver": {"name": "ザ・シェーバー", "cost": 500, "time_reduction": 0.05, "review_bonus": 1},
    },
    3: {
        "face": {"name": "スムースレイフェイスプロマックス", "cost": 3000, "time_reduction": 0.20, "review_bonus": 3},
        "body": {"name": "スムースレイボディプロマックス", "cost": 3000, "time_reduction": 0.20, "review_bonus": 3},
        "shaver": {"name": "高級シェーバー", "cost": 1500, "time_reduction": 0.10, "review_bonus": 2},
    },
    4: {
        "face": {"name": "スムースレイフェイスウルトラ", "cost": 10000, "time_reduction": 0.30, "review_bonus": 5},
        "body": {"name": "スムースレイボディウルトラ", "cost": 10000, "time_reduction": 0.30, "review_bonus": 5},
        "shaver": {"name": "プレミアムシェーバー", "cost": 5000, "time_reduction": 0.15, "review_bonus": 3},
    },
    5: {
        "face": {"name": "ニュースムースレイフェイス", "cost": 25000, "time_reduction": 0.45, "review_bonus": 8},
        "body": {"name": "ニュースムースレイボディ", "cost": 25000, "time_reduction": 0.45, "review_bonus": 8},
        "shaver": {"name": "エリートシェーバー", "cost": 12000, "time_reduction": 0.20, "review_bonus": 5},
    },
    6: {
        "face": {"name": "スムースレイオメガフェイス", "cost": 60000, "time_reduction": 0.60, "review_bonus": 10},
        "body": {"name": "スムースレイオメガボディ", "cost": 60000, "time_reduction": 0.60, "review_bonus": 10},
        "shaver": {"name": "オメガシェーバー", "cost": 30000, "time_reduction": 0.30, "review_bonus": 7},
    },
}

# 販売アイテム設定 (グレード別)
# type: reception(受付) / register(レジ)
# cost: 仕入れコスト, price: 販売価格追加, review_bonus: レビューボーナス, time_reduction: 施術時間短縮率
# 利益率を見直し（高すぎるの是正）
ITEM_CONFIG = {
    1: {
        "reception": [
            {"name": "冷却ジェル(低)", "cost": 20, "price": 30, "review_bonus": 1, "time_reduction": 0.02},
            {"name": "麻酔クリーム(低)", "cost": 40, "price": 60, "review_bonus": 1, "time_reduction": 0.03},
            {"name": "アメちゃん", "cost": 5, "price": 0, "review_bonus": 2, "time_reduction": 0.0},
            {"name": "ウォーターサーバー", "cost": 10, "price": 0, "review_bonus": 2, "time_reduction": 0.0},
        ],
        "register": [
            {"name": "アフターケアセット", "cost": 50, "price": 75, "review_bonus": 2, "time_reduction": 0.0},
            {"name": "粗品タオル", "cost": 20, "price": 0, "review_bonus": 1, "time_reduction": 0.0},
            {"name": "氷嚢", "cost": 15, "price": 0, "review_bonus": 1, "time_reduction": 0.01},
            {"name": "会員スタンプ", "cost": 5, "price": 0, "review_bonus": 2, "time_reduction": 0.0},
        ],
    },
    2: {
        "reception": [
            {"name": "冷却ジェル(普通)", "cost": 40, "price": 100, "review_bonus": 2, "time_reduction": 0.03},
            {"name": "麻酔クリーム(普通)", "cost": 60, "price": 140, "review_bonus": 2, "time_reduction": 0.04},
            {"name": "ストレスボール", "cost": 30, "price": 0, "review_bonus": 3, "time_reduction": 0.0},
        ],
        "register": [
            {"name": "保湿マスク", "cost": 50, "price": 120, "review_bonus": 3, "time_reduction": 0.0},
            {"name": "美容液", "cost": 80, "price": 200, "review_bonus": 3, "time_reduction": 0.0},
            {"name": "クーポン券", "cost": 5, "price": 0, "review_bonus": 2, "time_reduction": 0.0},
            {"name": "ARキット", "cost": 150, "price": 300, "review_bonus": 5, "time_reduction": 0.0},
        ],
    },
    3: {
        "reception": [
            {"name": "冷却ジェル(高)", "cost": 100, "price": 160, "review_bonus": 3, "time_reduction": 0.05},
            {"name": "麻酔クリーム(高)", "cost": 150, "price": 240, "review_bonus": 3, "time_reduction": 0.05},
            {"name": "笑気麻酔", "cost": 250, "price": 400, "review_bonus": 7, "time_reduction": 0.08},
            {"name": "敏感肌用保護ジェル", "cost": 120, "price": 180, "review_bonus": 4, "time_reduction": 0.05},
        ],
        "register": [
            {"name": "高級ローション", "cost": 120, "price": 200, "review_bonus": 4, "time_reduction": 0.0},
            {"name": "ルンバ", "cost": 400, "price": 600, "review_bonus": 8, "time_reduction": 0.0},
            {"name": "空気清浄機", "cost": 300, "price": 500, "review_bonus": 5, "time_reduction": 0.0},
        ],
    },
    4: {
        "reception": [
            {"name": "リラックスアロマ", "cost": 200, "price": 350, "review_bonus": 4, "time_reduction": 0.03},
        ],
        "register": [
            {"name": "高級乳液", "cost": 250, "price": 400, "review_bonus": 5, "time_reduction": 0.0},
            {"name": "ギフトカード", "cost": 300, "price": 450, "review_bonus": 6, "time_reduction": 0.0},
            {"name": "アロマディフューザー", "cost": 400, "price": 650, "review_bonus": 10, "time_reduction": 0.0},
            {"name": "ホームケアキット", "cost": 500, "price": 800, "review_bonus": 12, "time_reduction": 0.0},
        ],
    },
    5: {
        "reception": [],
        "register": [
            {"name": "VIPパック", "cost": 700, "price": 1200, "review_bonus": 15, "time_reduction": 0.0},
            {"name": "高級ギフトカード", "cost": 800, "price": 1300, "review_bonus": 18, "time_reduction": 0.0},
        ],
    },
    6: { # G5と同じアイテムプールを使用するか、無しにする (G6用の新アイテムは画像にないためG5のを流用orなし)
        "reception": [{"name": "VIPアロマ", "cost": 300, "price": 500, "review_bonus": 5, "time_reduction": 0.05}], 
        "register": [
            {"name": "プラチナ会員権", "cost": 1000, "price": 1800, "review_bonus": 35, "time_reduction": 0.0},
            {"name": "プラチナパック", "cost": 1200, "price": 1600, "review_bonus": 20, "time_reduction": 0.0},
            {"name": "プラチナギフトカード", "cost": 1500, "price": 2000, "review_bonus": 25, "time_reduction": 0.0},
        ]
    }, 
}

# ローン設定 (グレード別) - ローン使用時35日達成目標
# max_amount: 最大借入額, daily_rate: 日利(単利), term_days: 返済日数, grade_req: 必要グレード
LOAN_CONFIG = {
    "starter": {"max_amount": 8000, "daily_rate": 0.005, "term_days": 5, "grade_req": 1},
    "business": {"max_amount": 40000, "daily_rate": 0.005, "term_days": 10, "grade_req": 2}, # G2救済: 金利下げ期間延長
    "expert": {"max_amount": 300000, "daily_rate": 0.01, "term_days": 6, "grade_req": 3},
    "premium": {"max_amount": 1200000, "daily_rate": 0.012, "term_days": 7, "grade_req": 4},
    "elite": {"max_amount": 6000000, "daily_rate": 0.015, "term_days": 7, "grade_req": 5},
}


# ============================================
# データクラス
# ============================================

@dataclass
class Staff:
    rank: StaffRank
    
    def get_success_rate(self, customer_wealth: WealthLevel) -> float:
        return STAFF_SUCCESS_RATE.get((customer_wealth, self.rank), 0.5)
    
    def get_review_multiplier(self) -> float:
        return STAFF_REVIEW_MULTIPLIER[self.rank]
    
    def get_daily_salary(self) -> int:
        return STAFF_DAILY_SALARY[self.rank]

@dataclass
class Customer:
    wealth: WealthLevel
    plan_price: int
    
    @staticmethod
    def generate(star_rating: int, review_score: int) -> 'Customer':
        # レビュースコアから区間を決定
        if review_score < 20:
            score_bracket = 0
        elif review_score < 50:
            score_bracket = 20
        elif review_score < 80:
            score_bracket = 50
        else:
            score_bracket = 80
        
        # 分布から富裕層を決定
        dist = CUSTOMER_DISTRIBUTION.get(star_rating, {}).get(score_bracket, {WealthLevel.POOREST: 1.0})
        r = random.random()
        cumulative = 0
        wealth = WealthLevel.POOREST
        for w, prob in dist.items():
            cumulative += prob
            if r <= cumulative:
                wealth = w
                break
        
        # プランを選択
        plans = PLAN_PRICES.get(wealth, {"default": 20})
        plan_name = random.choice(list(plans.keys()))
        price = plans[plan_name]
        
        return Customer(wealth=wealth, plan_price=price)

@dataclass
class Loan:
    """ローン (単利計算)"""
    loan_type: str  # ローン種類名
    principal: int  # 元金
    daily_rate: float  # 日利 (単利)
    term_days: int  # 返済期間
    remaining_principal: int = 0  # 残り元金
    remaining_days: int = 0  # 残り日数
    accrued_interest: int = 0  # 累積利息
    
    def __post_init__(self):
        self.remaining_principal = self.principal
        self.remaining_days = self.term_days
        self.accrued_interest = 0
    
    def accrue_daily_interest(self):
        """日次利息を加算 (単利)"""
        daily_interest = int(self.remaining_principal * self.daily_rate)
        self.accrued_interest += daily_interest
        return daily_interest
    
    def get_minimum_payment(self) -> int:
        """最低返済額 (元金の一部 + 利息)"""
        if self.remaining_days <= 0:
            return self.remaining_principal + self.accrued_interest
        base_payment = self.remaining_principal // self.remaining_days
        return base_payment + self.accrued_interest
    
    def make_payment(self, amount: int = 0) -> int:
        """返済を行う。amount=0なら最低額、それ以上なら繰り上げ返済"""
        if self.remaining_principal <= 0:
            return 0
        
        # 利息を加算
        self.accrue_daily_interest()
        
        # 返済額決定
        min_payment = self.get_minimum_payment()
        actual_payment = max(min_payment, amount) if amount > 0 else min_payment
        
        # 利息から先に支払い
        if actual_payment >= self.accrued_interest:
            actual_payment -= self.accrued_interest
            interest_paid = self.accrued_interest
            self.accrued_interest = 0
        else:
            interest_paid = actual_payment
            self.accrued_interest -= actual_payment
            actual_payment = 0
        
        # 残りを元金返済
        principal_paid = min(actual_payment, self.remaining_principal)
        self.remaining_principal -= principal_paid
        
        self.remaining_days -= 1
        return interest_paid + principal_paid
    
    def is_paid_off(self) -> bool:
        return self.remaining_principal <= 0

@dataclass
class SalonState:
    grade: int = 1
    money: int = 3000  # 初期資金増額 (G1短縮のため)
    cumulative_review: int = 0
    star_rating: int = 1
    attraction_level: int = 50  # 集客度 (G1上限100で50開始 = 10人)
    day: int = 1
    staff: List[Staff] = field(default_factory=list)
    loans: List[Loan] = field(default_factory=list)
    active_ads: List[dict] = field(default_factory=list)  # {"type": str, "remaining": int, "boost": float}
    tool_grade: int = 1  # 現在所有のツールグレード
    
    # 統計
    total_revenue: int = 0
    total_expenses: int = 0
    customers_served: int = 0
    
    def get_total_time_reduction(self) -> float:
        """現在のツールによる総施術時間短縮率を取得"""
        if self.tool_grade not in TOOL_CONFIG:
            return 0.0
        
        tools = TOOL_CONFIG[self.tool_grade]
        total_reduction = 0.0
        # 各部位の短縮率を加算 (例: フェイス0.1 + ボディ0.1 + シェーバー0.05 = 0.25)
        # ※本来は施術内容によるが、簡易的に全施術に適用される効率係数とする
        for tool_data in tools.values():
            total_reduction += tool_data.get("time_reduction", 0.0)
            
        return total_reduction
    
    def get_tool_review_bonus(self) -> int:
        """現在のツールによるレビューボーナスを取得"""
        if self.tool_grade not in TOOL_CONFIG:
            return 0
        tools = TOOL_CONFIG[self.tool_grade]
        return sum(t["review_bonus"] for t in tools.values())
    
    def get_beds(self) -> int:
        return GRADE_CONFIG[self.grade]["beds"]
    
    def get_staff_slots(self) -> int:
        return GRADE_CONFIG[self.grade]["staff_slots"]
    
    def get_rent(self) -> int:
        return GRADE_CONFIG[self.grade]["rent"]
    
    def get_required_stars(self) -> int:
        return GRADE_CONFIG[self.grade]["required_stars"]
    
    def get_attraction_cap(self) -> int:
        """現在のグレードの集客度上限を取得"""
        return GRADE_CONFIG[self.grade]["attraction_cap"]
    
    def get_max_customers(self) -> int:
        """現在のグレードと設置アイテム効果に基づく最大顧客数を取得"""
        base_max = GRADE_CONFIG[self.grade]["max_customers"]
        
        # 設置アイテムによる集客ブースト (空気清浄機、ルンバなど)
        # G1-G2: +0%, G3: +10%, G4: +15%, G5: +20%, G6: +30%
        facility_boost = {1: 0.0, 2: 0.0, 3: 0.10, 4: 0.15, 5: 0.20, 6: 0.30, 7: 0.30}
        boost = facility_boost.get(self.grade, 0.0)
        
        effective_max = int(base_max * (1.0 + boost))
        return effective_max
    
    def get_current_customers(self) -> int:
        """現在の集客度から実際の集客数を計算"""
        cap = self.get_attraction_cap()
        max_cust = self.get_max_customers()
        # 集客度 / 上限 * 集客数上限
        ratio = min(self.attraction_level / cap, 1.0)
        return int(max_cust * ratio)
    
    def update_star_rating(self):
        for stars, threshold in sorted(REVIEW_THRESHOLDS.items(), reverse=True):
            if self.cumulative_review >= threshold:
                self.star_rating = stars
                break

# ============================================
# シミュレーション
# ============================================

class SalonSimulator:
    def __init__(self, config: dict = None):
        self.state = SalonState()
        self.config = config or {}
        
        # シミュレーション設定
        self.operating_time = self.config.get("operating_time", 600)  # 10分 = 600秒
        self.avg_treatment_time = self.config.get("avg_treatment_time", 15)  # 平均施術時間
        self.use_loans = self.config.get("use_loans", False)
        self.verbose = self.config.get("verbose", False)
    
    def _select_best_ad(self, exclude_types: list = None) -> str:
        """現在のグレードで使用可能な最適な広告を選択"""
        if exclude_types is None:
            exclude_types = []
        available_ads = []
        for ad_name, ad_config in AD_CONFIG.items():
            if ad_name == "none" or ad_name in exclude_types:
                continue
            if ad_config["grade_req"] <= self.state.grade:
                # コスト効率 = attraction_boost / cost (無料は最優先)
                if ad_config["cost"] == 0:
                    efficiency = 100.0
                else:
                    efficiency = (ad_config["attraction_boost"] * ad_config["duration"]) / ad_config["cost"] * 100
                available_ads.append((ad_name, efficiency))
        
        if not available_ads:
            return None
        
        # 効率順にソートして、ランダム要素を加える
        available_ads.sort(key=lambda x: x[1], reverse=True)
        # 上位3つからランダム選択
        top_ads = available_ads[:min(3, len(available_ads))]
        return random.choice(top_ads)[0]
    
    def simulate_day(self) -> dict:
        """1日をシミュレート"""
        daily_revenue = 0
        daily_expenses = 0
        daily_customers = 0
        daily_reviews = []
        
        # 詳細経費追跡
        expense_ad = 0
        expense_rent = 0
        expense_staff = 0
        expense_loan = 0
        expense_items = 0
        new_loan_taken = None
        new_ad_started = None
        
        # アクティブ広告の効果計算とdecay (集客度ポイント)
        total_ad_boost = 0  # 広告による一時的集客度ブースト
        ads_to_remove = []
        for i, ad in enumerate(self.state.active_ads):
            total_ad_boost += ad["current_boost"]
            ad["current_boost"] -= AD_CONFIG[ad["type"]]["decay"]  # 日次減衰
            ad["remaining"] -= 1
            if ad["remaining"] <= 0 or ad["current_boost"] <= 0:
                ads_to_remove.append(i)
        
        # 期限切れ広告を削除
        for i in reversed(ads_to_remove):
            self.state.active_ads.pop(i)
        
        # 新しい広告を購入 (最大3つまで、同種類は不可)
        # 集客度がMAXに近づくよう積極的に広告を購入
        active_ad_types = [ad["type"] for ad in self.state.active_ads]
        attraction_ratio = self.state.attraction_level / self.state.get_attraction_cap()
        
        # 集客度が80%未満なら積極的に広告購入、3つまで購入可能
        while len(self.state.active_ads) < 3:
            if attraction_ratio >= 0.9 and len(self.state.active_ads) >= 1:
                break  # 90%以上なら追加広告不要
            best_ad = self._select_best_ad(exclude_types=active_ad_types)
            if best_ad is None:
                break
            ad_config = AD_CONFIG[best_ad]
            # 資金の20%以下なら購入（より積極的）
            if ad_config["cost"] <= self.state.money * 0.2 or ad_config["cost"] == 0:
                expense_ad += ad_config["cost"]
                self.state.active_ads.append({
                    "type": best_ad,
                    "remaining": ad_config["duration"],
                    "current_boost": ad_config["attraction_boost"]
                })
                active_ad_types.append(best_ad)
                total_ad_boost += ad_config["attraction_boost"]
                if new_ad_started is None:
                    new_ad_started = best_ad
            else:
                break
        
        daily_expenses += expense_ad
        
        # 家賃 (3日おき)
        if self.state.day % 3 == 0:
            expense_rent = self.state.get_rent()
            daily_expenses += expense_rent
        
        # スタッフ給料
        for staff in self.state.staff:
            expense_staff += staff.get_daily_salary()
        daily_expenses += expense_staff
        
        # ローン返済
        loans_to_remove = []
        for i, loan in enumerate(self.state.loans):
            payment = loan.make_payment()
            expense_loan += payment
            if loan.is_paid_off():
                loans_to_remove.append(i)
        for i in reversed(loans_to_remove):
            self.state.loans.pop(i)
        daily_expenses += expense_loan
        
        # 来客数計算 (集客度システム)
        # 広告効果は一時的に集客度を上げる（直接ポイント）
        daily_variance = random.randint(-3, 3)  # +-3の日次変動
        effective_attraction = self.state.attraction_level + total_ad_boost + daily_variance
        effective_attraction = max(10, min(effective_attraction, self.state.get_attraction_cap()))
        
        # 集客度から集客数を計算
        cap = self.state.get_attraction_cap()
        max_customers = self.state.get_max_customers()
        customer_ratio = effective_attraction / cap
        expected_customers = int(max_customers * customer_ratio)
        
        # 施術シミュレーション
        beds = self.state.get_beds()
        available_staff = len(self.state.staff)
        player_beds = 1  # VIP用
        staff_beds = min(beds - player_beds, available_staff)
        
        for _ in range(expected_customers):
            customer = Customer.generate(self.state.star_rating, self.state.cumulative_review % 100)
            daily_customers += 1
            
            # ベッド割り当て（ランダム）
            is_vip_treatment = random.random() < (player_beds / beds) if beds > 0 else True
            
            if is_vip_treatment:
                success = True
                review_multiplier = 1.0
            else:
                if self.state.staff:
                    staff = random.choice(self.state.staff)
                    success = random.random() < staff.get_success_rate(customer.wealth)
                    review_multiplier = staff.get_review_multiplier()
                else:
                    success = True
                    review_multiplier = 1.0
            
            if success:
                daily_revenue += customer.plan_price
                
                # アイテム適用 (受付1つ + レジ1つ)
                if self.state.grade in ITEM_CONFIG:
                    items = ITEM_CONFIG[self.state.grade]
                    # 受付アイテム
                    if items.get("reception"):
                        reception_item = random.choice(items["reception"])
                        expense_items += reception_item["cost"]
                        daily_revenue += reception_item["price"]
                        daily_reviews.append(int(reception_item["review_bonus"] * review_multiplier))
                    # レジアイテム
                    if items.get("register"):
                        register_item = random.choice(items["register"])
                        expense_items += register_item["cost"]
                        daily_revenue += register_item["price"]
                        daily_reviews.append(int(register_item["review_bonus"] * review_multiplier))
                
                # ツールレビューボーナス
                tool_review_bonus = self.state.get_tool_review_bonus()
                if tool_review_bonus > 0:
                    daily_reviews.append(int(tool_review_bonus * review_multiplier))
                
                # 基本レビュー (-50 ~ +50, 成功時は通常50、稀に下がる。平均約40)
                # 80%で+50, 15%で+30~+49, 5%で-10~+29
                roll = random.random()
                if roll < 0.80:
                    base_review = 50  # 完璧な施術
                elif roll < 0.95:
                    base_review = random.randint(30, 49)  # 良い施術
                else:
                    base_review = random.randint(-10, 29)  # 普通～微妙
                daily_reviews.append(int(base_review * review_multiplier))
            else:
                # 失敗時: -50 ~ 0
                base_review = random.randint(-50, 0)
                daily_reviews.append(int(base_review * review_multiplier))
        
        # 集客度更新 (良いレビューで上昇、上限はグレードのcap)
        # 顧客数ベースで平均を計算（エントリ数ではなく）
        total_review = sum(daily_reviews)
        avg_review_per_customer = total_review / daily_customers if daily_customers > 0 else 0
        attraction_change = int((avg_review_per_customer / 5) * self.state.grade)  # グレード乗算: G6で平均40→+48ポイント
        new_attraction = self.state.attraction_level + attraction_change
        self.state.attraction_level = max(10, min(new_attraction, self.state.get_attraction_cap()))
        
        # 今日のレビュー合計
        today_review_total = sum(daily_reviews)
        
        # レビュー累積
        self.state.cumulative_review += today_review_total
        self.state.cumulative_review = max(0, self.state.cumulative_review)
        self.state.update_star_rating()
        
        # 収支更新
        net = daily_revenue - daily_expenses
        self.state.money += net
        self.state.total_revenue += daily_revenue
        self.state.total_expenses += daily_expenses
        self.state.customers_served += daily_customers
        self.state.day += 1
        
        return {
            "day": self.state.day - 1,
            "grade": self.state.grade,
            "stars": self.state.star_rating,
            "revenue": daily_revenue,
            "expenses": daily_expenses,
            "net": net,
            "money": self.state.money,
            # 詳細経費
            "expense_ad": expense_ad,
            "expense_rent": expense_rent,
            "expense_staff": expense_staff,
            "expense_loan": expense_loan,
            "expense_items": expense_items,
            # スタッフ情報
            "staff_count": len(self.state.staff),
            # ローン情報
            "active_loans": len([l for l in self.state.loans if l.remaining_days > 0]),
            "new_loan": new_loan_taken,
            # 広告情報
            "new_ad": new_ad_started,
            "active_ads_count": len(self.state.active_ads),
            "ad_boost": total_ad_boost,
            # 顧客情報
            "customers": daily_customers,
            "max_customers": max_customers,
            "attraction_level": self.state.attraction_level,
            "attraction_cap": self.state.get_attraction_cap(),
            # レビュー情報
            "today_review": today_review_total,
            "review_total": self.state.cumulative_review,
        }
    
    def try_upgrade(self) -> tuple:
        """グレードアップを試みる。Returns (upgraded, loan_info)"""
        if self.state.grade >= MAX_GRADE:
            return False, None
        
        next_grade = self.state.grade + 1
        config = GRADE_CONFIG[next_grade]
        
        if self.state.star_rating < config["required_stars"]:
            return False, None
            
        # ツールコスト計算 (自動購入)
        tool_cost = 0
        if next_grade in TOOL_CONFIG:
            for tool in TOOL_CONFIG[next_grade].values():
                tool_cost += tool["cost"]
        # 全ベッド分
        tool_cost *= config["beds"]
        
        # 総コスト (アップグレード費用 + ツール一式)
        total_cost = config["upgrade_cost"] + tool_cost
        
        loan_info = None
        
        # コストチェック
        if self.state.money < total_cost:
            if not self.use_loans:
                return False, None
            
            # 現在の未返済ローン種類を取得（同種類は完済まで借りられない）
            active_loan_types = [loan.loan_type for loan in self.state.loans if not loan.is_paid_off()]
            
            # 使用可能な最適なローンを選択（同種類チェック）
            needed = total_cost - self.state.money
            best_loan = None
            
            for loan_name, loan_cfg in LOAN_CONFIG.items():
                if loan_name in active_loan_types:
                    continue  # 同種類のローンは借りられない
                if loan_cfg["grade_req"] <= self.state.grade:
                    if loan_cfg["max_amount"] >= needed:
                        if best_loan is None or loan_cfg["daily_rate"] < LOAN_CONFIG[best_loan]["daily_rate"]:
                            best_loan = loan_name
            
            if best_loan is None:
                # 複数ローンを組む (同種類は不可)
                remaining_needed = needed
                loans_taken = []
                for loan_name, loan_cfg in sorted(LOAN_CONFIG.items(), key=lambda x: -x[1]["max_amount"]):
                    # 同種類のローンが既にあるか確認
                    if loan_name in active_loan_types:
                        continue
                    if loan_cfg["grade_req"] <= self.state.grade and remaining_needed > 0:
                        borrow_amount = min(loan_cfg["max_amount"], remaining_needed)
                        loan = Loan(
                            loan_type=loan_name,
                            principal=borrow_amount,
                            daily_rate=loan_cfg["daily_rate"],
                            term_days=loan_cfg["term_days"]
                        )
                        self.state.loans.append(loan)
                        self.state.money += borrow_amount
                        remaining_needed -= borrow_amount
                        loans_taken.append({"type": loan_name, "amount": borrow_amount, "term": loan_cfg["term_days"]})
                
                if remaining_needed > 0:
                    return False, None
                loan_info = {"loans": loans_taken, "total": needed}
            else:
                # 単一ローンで借りる
                loan_cfg = LOAN_CONFIG[best_loan]
                loan = Loan(
                    loan_type=best_loan,
                    principal=needed,
                    daily_rate=loan_cfg["daily_rate"],
                    term_days=loan_cfg["term_days"]
                )
                self.state.loans.append(loan)
                self.state.money += needed
                loan_info = {"type": best_loan, "principal": needed, "term": loan_cfg["term_days"], "rate": loan_cfg["daily_rate"]}
        
        
        # アップグレード実行
        self.state.grade = next_grade
        self.state.money -= config["upgrade_cost"]
        
        # ツール更新と支払い
        self.state.money -= tool_cost
        self.state.tool_grade = next_grade
        
        # スタッフランク設定（グレードに応じたランクで雇用/昇格）
        
        # スタッフランク設定（グレードに応じたランクで雇用/昇格）
        # G1-2: 大学生, G3: 新卒, G4: 中堅, G5: ベテラン, G6-7: プロ
        staff_rank_by_grade = {
            1: StaffRank.STUDENT,
            2: StaffRank.STUDENT,
            3: StaffRank.NEWBIE,
            4: StaffRank.REGULAR,
            5: StaffRank.VETERAN,
            6: StaffRank.PRO,
            7: StaffRank.PRO,
        }
        hire_rank = staff_rank_by_grade.get(next_grade, StaffRank.NEWBIE)
        
        # 既存スタッフを新ランクに昇格
        for staff in self.state.staff:
            staff.rank = hire_rank
        
        # 新スタッフ追加
        while len(self.state.staff) < config["staff_slots"]:
            self.state.staff.append(Staff(rank=hire_rank))
        
        return True, loan_info
    
    def run(self, max_days: int = 100) -> List[dict]:
        """シミュレーション実行"""
        results = []
        
        for _ in range(max_days):
            result = self.simulate_day()
            results.append(result)
            
            if self.verbose:
                r = result
                # 基本情報
                print(f"Day {r['day']}: G{r['grade']} *{r['stars']} | "
                      f"Money: ${r['money']} | "
                      f"Customers: {r['customers']}/{r['max_customers']} (Attr:{r['attraction_level']}/{r['attraction_cap']})")
                # 詳細 (経費がある場合のみ)
                expenses = []
                if r['expense_rent'] > 0:
                    expenses.append(f"Rent:${r['expense_rent']}")
                if r['expense_staff'] > 0:
                    expenses.append(f"Staff({r['staff_count']}):${r['expense_staff']}")
                if r['expense_loan'] > 0:
                    expenses.append(f"Loan:${r['expense_loan']}")
                if r['expense_ad'] > 0:
                    expenses.append(f"Ad:${r['expense_ad']}")
                if expenses:
                    print(f"       Expenses: {' | '.join(expenses)}")
                print(f"       Revenue:${r['revenue']} Net:${r['net']} Review:+{r['today_review']} (Total:{r['review_total']})")
            
            # アップグレード試行
            upgraded, loan_info = self.try_upgrade()
            if upgraded:
                result['upgraded'] = True
                if loan_info:
                    result['loan_info'] = loan_info
                    # 借入額を記録
                    if "total" in loan_info:
                        result['borrowed_amount'] = loan_info['total']
                    else:
                        result['borrowed_amount'] = loan_info['principal']
                
                if self.verbose:
                    msg = f"  >>> Upgraded to Grade {self.state.grade}!"
                    if loan_info:
                        if "loans" in loan_info:
                            msg += f" [LOANS: ${loan_info['total']} ({len(loan_info['loans'])} loans)]"
                        else:
                            msg += f" [LOAN: {loan_info['type']} ${loan_info['principal']} @{loan_info['rate']*100:.1f}%/day for {loan_info['term']}d]"
                    print(msg)
            
            # 目標達成チェック
            if self.state.grade >= MAX_GRADE:
                if self.verbose:
                    print(f"\n*** Reached Grade {MAX_GRADE} on Day {result['day']}! ***")
                break
        
        return results

# ============================================
# 実行
# ============================================

def main():
    print("=" * 60)
    print("Hair Removal Salon Simulator")
    print("=" * 60)
    
    # 複数回シミュレーション (ローンなし)
    num_runs = 1000
    days_to_grade7 = []
    
    for i in range(num_runs):
        sim = SalonSimulator(config={
            "operating_time": 600,
            "avg_treatment_time": 15,
            "use_loans": False,
            "verbose": False,
        })
        results = sim.run(max_days=100)
        
        if sim.state.grade >= 7:
            days_to_grade7.append(results[-1]["day"])
        else:
            days_to_grade7.append(100)  # 未達成
    
    print(f"\n[Result] Simulation NO LOANS ({num_runs} runs)")
    print("-" * 40)
    print(f"Days to Grade 7:")
    print(f"  Average: {sum(days_to_grade7) / len(days_to_grade7):.1f} days")
    print(f"  Min: {min(days_to_grade7)} days")
    print(f"  Max: {max(days_to_grade7)} days")
    
    # 複数回シミュレーション (ローンあり)
    days_to_grade7_loans = []
    
    for i in range(num_runs):
        sim = SalonSimulator(config={
            "operating_time": 600,
            "avg_treatment_time": 15,
            "use_loans": True,
            "verbose": False,
        })
        results = sim.run(max_days=100)
        
        if sim.state.grade >= 7:
            days_to_grade7_loans.append(results[-1]["day"])
        else:
            days_to_grade7_loans.append(100)  # 未達成
    
    print(f"\n[Result] Simulation WITH LOANS ({num_runs} runs)")
    print("-" * 40)
    print(f"Days to Grade 7:")
    print(f"  Average: {sum(days_to_grade7_loans) / len(days_to_grade7_loans):.1f} days")
    print(f"  Min: {min(days_to_grade7_loans)} days")
    print(f"  Max: {max(days_to_grade7_loans)} days")
    
    # 詳細シミュレーション (ローンなし)
    print("\n" + "=" * 60)
    print("Detailed Simulation (NO LOANS)")
    print("=" * 60)
    
    sim_no_loan = SalonSimulator(config={
        "operating_time": 600,
        "avg_treatment_time": 15,
        "use_loans": False,
        "verbose": True,
    })
    results_no_loan = sim_no_loan.run(max_days=50)
    
    print("\n[Stats] Final Statistics (No Loans):")
    print(f"  Total Revenue: ${sim_no_loan.state.total_revenue}")
    print(f"  Total Expenses: ${sim_no_loan.state.total_expenses}")
    print(f"  Net Profit: ${sim_no_loan.state.total_revenue - sim_no_loan.state.total_expenses}")
    print(f"  Final Money: ${sim_no_loan.state.money}")
    print(f"  Final Grade: {sim_no_loan.state.grade}")
    print(f"  Days: {results_no_loan[-1]['day']}")
    
    # 詳細シミュレーション (ローンあり)
    print("\n" + "=" * 60)
    print("Detailed Simulation (WITH LOANS)")
    print("=" * 60)
    
    sim_loan = SalonSimulator(config={
        "operating_time": 600,
        "avg_treatment_time": 15,
        "use_loans": True,
        "verbose": True,
    })
    results_loan = sim_loan.run(max_days=50)
    
    print("\n[Stats] Final Statistics (With Loans):")
    print(f"  Total Revenue: ${sim_loan.state.total_revenue}")
    print(f"  Total Expenses: ${sim_loan.state.total_expenses}")
    print(f"  Net Profit: ${sim_loan.state.total_revenue - sim_loan.state.total_expenses}")
    print(f"  Final Money: ${sim_loan.state.money}")
    print(f"  Final Grade: {sim_loan.state.grade}")
    print(f"  Days: {results_loan[-1]['day']}")
    print(f"  Active Loans: {len([l for l in sim_loan.state.loans if l.remaining_days > 0])}")
    
    # 比較
    print("\n" + "=" * 60)
    print("COMPARISON: No Loans vs With Loans")
    print("=" * 60)
    days_no = results_no_loan[-1]['day'] if sim_no_loan.state.grade >= MAX_GRADE else 50
    days_yes = results_loan[-1]['day'] if sim_loan.state.grade >= MAX_GRADE else 50
    print(f"  Days to Grade {MAX_GRADE}: {days_no} (no loans) vs {days_yes} (with loans)")
    print(f"  Time saved: {days_no - days_yes} days")
    
    # CSV出力
    import csv
    csv_path = "simulation_results.csv"
    with open(csv_path, 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        writer.writerow([
            "Day", "Grade", "Stars", "Money", "Customers", "MaxCustomers", 
            "基本集客度", "広告ブースト", "集客度上限", "Revenue", "Rent", "StaffCount", "StaffCost", "LoanRepayment", "Ad", "Items",
            "TotalExpenses", "NetProfit", "TodayReview", "CumulativeReview",
            "UpgradeCost", "Upgraded", "Borrowed"
        ])
        
        for r in results_loan:
            # 次のグレードのアップグレードコストを取得
            next_grade = r['grade'] + 1 if r['grade'] < MAX_GRADE else r['grade']
            upgrade_cost = GRADE_CONFIG[next_grade]["upgrade_cost"] if next_grade <= MAX_GRADE else 0
            borrowed = r.get('borrowed_amount', 0)
            writer.writerow([
                r['day'], r['grade'], r['stars'], r['money'],
                r['customers'], r['max_customers'], 
                r['attraction_level'], int(r['ad_boost']), r['attraction_cap'],
                r['revenue'], r['expense_rent'], r['staff_count'], r['expense_staff'], 
                r['expense_loan'], r['expense_ad'], r['expense_items'],
                r['expenses'], r['revenue'] - r['expenses'],
                r['today_review'], r['review_total'],
                upgrade_cost, "Yes" if r.get('upgraded') else "",
                borrowed if borrowed > 0 else ""
            ])
    
    print(f"\n[CSV] Detailed results exported to: {csv_path}")
    
    # 日次表出力
    print("\n" + "=" * 80)
    print("DAILY BREAKDOWN (WITH LOANS)")
    print("=" * 80)
    print(f"{'Day':>4} | {'G':>2} | {'*':>2} | {'Money':>10} | {'Cust':>5} | {'Revenue':>8} | {'Expenses':>8} | {'Net':>8} | {'Review':>8} | Event")
    print("-" * 80)
    for r in results_loan:
        event = ""
        if r.get('upgraded'):
            event = f"Upgrade to G{r['grade']}"
        if r.get('loan_info'):
            event += " +Loan"
        print(f"{r['day']:>4} | G{r['grade']:>1} | {r['stars']:>2} | ${r['money']:>9} | {r['customers']:>5} | ${r['revenue']:>7} | ${r['expenses']:>7} | ${r['revenue']-r['expenses']:>7} | +{r['today_review']:>6} | {event}")

if __name__ == "__main__":
    main()
