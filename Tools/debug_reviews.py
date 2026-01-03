"""Debug script to investigate review fluctuation"""
from salon_simulator import SalonSimulator, MAX_GRADE

sim = SalonSimulator(config={
    'operating_time': 450,
    'use_loans': False,
    'verbose': False,
})

print("Day | G | * | Cust | Max | Capacity | Review | Total | Notes")
print("-" * 70)

for day in range(1, 31):
    result = sim.simulate_day()
    upgraded, _ = sim.try_upgrade()
    
    notes = []
    if upgraded:
        notes.append(f"->G{sim.state.grade}")
    if result['today_review'] < 0:
        notes.append("NEGATIVE!")
    
    print(f"{day:3} | {result['grade']} | {result['stars']:2} | {result['customers']:4} | {result['max_customers']:3} | - | {result['today_review']:+6} | {result['review_total']:6} | {' '.join(notes)}")
