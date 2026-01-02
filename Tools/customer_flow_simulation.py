"""
Hair Removal Salon - Customer Flow Simulation
Calculates optimal MaxCustomers and chair counts per grade
"""

import random

# Configuration
BUSINESS_TIME = 600  # 10 minutes in seconds

# Grade data: (totalCustomers, beds)
GRADES = {
    1: (18, 1),
    2: (36, 2),
    3: (79, 4),
    4: (124, 6),
    5: (172, 8),
    6: (286, 10),
    7: (345, 12),
}

# Time parameters (seconds)
TREATMENT_TIME_MIN = 10
TREATMENT_TIME_MAX = 30
RECEPTION_TIME_MIN = 5
RECEPTION_TIME_MAX = 10
CASHIER_TIME_MIN = 5
CASHIER_TIME_MAX = 10
MOVE_TIME = 3  # Time to move between areas
WAIT_TIMEOUT = 75  # Average of 60-90 seconds

def simulate_grade(grade, total_customers, beds, num_runs=10):
    """Simulate customer flow for a grade and return statistics"""
    
    results = []
    
    for run in range(num_runs):
        spawn_interval = BUSINESS_TIME / total_customers
        
        # Track queues and states
        reception_queue = []  # List of (arrival_time, customer_id)
        waiting_queue = []    # Waiting for treatment
        cashier_queue = []    # Waiting for payment
        
        bed_free_at = [0] * beds  # When each bed becomes free
        reception_free_at = 0
        cashier_free_at = 0
        
        # Stats
        max_reception = 0
        max_waiting = 0
        max_cashier = 0
        max_simultaneous = 0
        customers_served = 0
        customers_left = 0
        
        # Active customers in the shop
        active_customers = []  # (customer_id, exit_time)
        
        # Simulate each customer arrival
        for i in range(total_customers):
            arrival_time = i * spawn_interval + random.uniform(-spawn_interval*0.3, spawn_interval*0.3)
            arrival_time = max(0, arrival_time)
            
            # Customer flow times
            reception_wait_start = arrival_time + MOVE_TIME
            reception_start = max(reception_wait_start, reception_free_at)
            reception_time = random.uniform(RECEPTION_TIME_MIN, RECEPTION_TIME_MAX)
            reception_end = reception_start + reception_time
            reception_free_at = reception_end
            
            # Check if customer waited too long at reception
            if reception_start - reception_wait_start > WAIT_TIMEOUT:
                customers_left += 1
                continue
            
            # Track reception queue size
            reception_queue_size = sum(1 for t in active_customers if t[1] > reception_wait_start)
            max_reception = max(max_reception, int((reception_start - reception_wait_start) / spawn_interval) + 1)
            
            # Waiting for bed
            waiting_start = reception_end + MOVE_TIME
            earliest_bed = min(range(beds), key=lambda b: bed_free_at[b])
            bed_available = bed_free_at[earliest_bed]
            treatment_start = max(waiting_start, bed_available)
            
            # Check if customer waited too long for bed
            if treatment_start - waiting_start > WAIT_TIMEOUT:
                customers_left += 1
                continue
            
            # Track waiting queue size
            waiting_time = treatment_start - waiting_start
            max_waiting = max(max_waiting, int(waiting_time / spawn_interval) + 1)
            
            # Treatment
            treatment_time = random.uniform(TREATMENT_TIME_MIN, TREATMENT_TIME_MAX)
            treatment_end = treatment_start + treatment_time
            bed_free_at[earliest_bed] = treatment_end
            
            # Cashier
            cashier_wait_start = treatment_end + MOVE_TIME
            cashier_start = max(cashier_wait_start, cashier_free_at)
            
            # Check if customer waited too long at cashier
            if cashier_start - cashier_wait_start > WAIT_TIMEOUT:
                customers_left += 1
                continue
            
            # Track cashier queue size
            cashier_wait_time = cashier_start - cashier_wait_start
            max_cashier = max(max_cashier, int(cashier_wait_time / spawn_interval) + 1)
            
            cashier_time = random.uniform(CASHIER_TIME_MIN, CASHIER_TIME_MAX)
            cashier_end = cashier_start + cashier_time
            cashier_free_at = cashier_end
            
            # Customer exits
            exit_time = cashier_end + MOVE_TIME
            active_customers.append((i, exit_time))
            customers_served += 1
            
            # Calculate simultaneous customers at this point
            current_time = arrival_time
            simultaneous = sum(1 for c in active_customers if c[1] > current_time)
            max_simultaneous = max(max_simultaneous, simultaneous)
        
        # Clean up - calculate final max simultaneous
        for t in range(0, int(BUSINESS_TIME), 5):
            simultaneous = 0
            for i in range(total_customers):
                arrival = i * spawn_interval
                # Estimate stay time
                stay_time = MOVE_TIME * 4 + (RECEPTION_TIME_MIN + RECEPTION_TIME_MAX) / 2 + \
                           (TREATMENT_TIME_MIN + TREATMENT_TIME_MAX) / 2 + \
                           (CASHIER_TIME_MIN + CASHIER_TIME_MAX) / 2
                if arrival <= t < arrival + stay_time:
                    simultaneous += 1
            max_simultaneous = max(max_simultaneous, simultaneous)
        
        results.append({
            'max_reception': max_reception,
            'max_waiting': max_waiting,
            'max_cashier': max_cashier,
            'max_simultaneous': max_simultaneous,
            'served': customers_served,
            'left': customers_left,
        })
    
    # Average results
    avg = {
        'max_reception': max(r['max_reception'] for r in results),
        'max_waiting': max(r['max_waiting'] for r in results),
        'max_cashier': max(r['max_cashier'] for r in results),
        'max_simultaneous': max(r['max_simultaneous'] for r in results),
        'avg_served': sum(r['served'] for r in results) / num_runs,
        'avg_left': sum(r['left'] for r in results) / num_runs,
    }
    
    return avg

def calculate_theoretical():
    """Calculate theoretical values based on queuing theory"""
    print("=" * 80)
    print("THEORETICAL CALCULATION (Queue Theory)")
    print("=" * 80)
    
    # Average times
    avg_treatment = (TREATMENT_TIME_MIN + TREATMENT_TIME_MAX) / 2  # 20s
    avg_reception = (RECEPTION_TIME_MIN + RECEPTION_TIME_MAX) / 2  # 7.5s
    avg_cashier = (CASHIER_TIME_MIN + CASHIER_TIME_MAX) / 2  # 7.5s
    avg_move = MOVE_TIME  # 3s
    
    # Total stay time per customer
    total_stay = avg_move * 4 + avg_reception + avg_treatment + avg_cashier
    print(f"\nAverage customer stay time: {total_stay:.1f}s")
    print(f"  - Movement: {avg_move * 4}s")
    print(f"  - Reception: {avg_reception}s")
    print(f"  - Treatment: {avg_treatment}s")
    print(f"  - Cashier: {avg_cashier}s")
    
    print("\n" + "-" * 80)
    print(f"{'Grade':>5} | {'Customers':>9} | {'Beds':>4} | {'Interval':>8} | {'MaxCust':>7} | {'Reception':>9} | {'Waiting':>7} | {'Cashier':>7} | {'Bed Cap':>7}")
    print("-" * 80)
    
    recommendations = []
    
    for grade, (customers, beds) in GRADES.items():
        spawn_interval = BUSINESS_TIME / customers
        
        # Simultaneous customers = stay_time / spawn_interval
        max_simultaneous = int(total_stay / spawn_interval) + 2  # +2 for buffer
        
        # Bed capacity per day
        bed_capacity = int(BUSINESS_TIME / (avg_treatment + avg_move * 2)) * beds
        
        # Reception chairs: 1 receptionist, queue builds when faster than processing
        # If spawn_interval < avg_reception, queue builds
        if spawn_interval < avg_reception:
            reception_queue = int((avg_reception - spawn_interval) * customers / avg_reception) + 1
        else:
            reception_queue = 1
        reception_chairs = min(reception_queue, max_simultaneous // 2)
        
        # Waiting chairs: depends on bed availability
        # If customers arrive faster than beds can process
        arrivals_per_treatment = avg_treatment / spawn_interval
        waiting_chairs = max(2, int(arrivals_per_treatment * 1.5))
        waiting_chairs = min(waiting_chairs, beds * 2)  # Cap at 2x beds
        
        # Cashier chairs: similar to reception
        if spawn_interval < avg_cashier:
            cashier_queue = int((avg_cashier - spawn_interval) * customers / avg_cashier) + 1
        else:
            cashier_queue = 1
        cashier_chairs = min(cashier_queue, max_simultaneous // 3)
        
        print(f"{grade:>5} | {customers:>9} | {beds:>4} | {spawn_interval:>7.1f}s | {max_simultaneous:>7} | {reception_chairs:>9} | {waiting_chairs:>7} | {cashier_chairs:>7} | {bed_capacity:>7}")
        
        recommendations.append({
            'grade': grade,
            'customers': customers,
            'beds': beds,
            'max_simultaneous': max_simultaneous,
            'reception_chairs': reception_chairs,
            'waiting_chairs': waiting_chairs,
            'cashier_chairs': cashier_chairs,
            'bed_capacity': bed_capacity,
        })
    
    return recommendations

def main():
    print("\n" + "=" * 80)
    print("HAIR REMOVAL SALON - CUSTOMER FLOW SIMULATION")
    print("=" * 80)
    
    # Theoretical calculation
    recommendations = calculate_theoretical()
    
    # Summary table
    print("\n" + "=" * 80)
    print("RECOMMENDED SETTINGS")
    print("=" * 80)
    print(f"{'Grade':>5} | {'MaxCustomers':>12} | {'Reception':>9} | {'Waiting':>7} | {'Cashier':>7} | {'Total Chairs':>12}")
    print("-" * 80)
    
    for r in recommendations:
        total_chairs = r['reception_chairs'] + r['waiting_chairs'] + r['cashier_chairs']
        print(f"{r['grade']:>5} | {r['max_simultaneous']:>12} | {r['reception_chairs']:>9} | {r['waiting_chairs']:>7} | {r['cashier_chairs']:>7} | {total_chairs:>12}")
    
    # Warning for high grades
    print("\n" + "-" * 80)
    print("NOTES:")
    for r in recommendations:
        if r['bed_capacity'] < r['customers']:
            shortage = r['customers'] - r['bed_capacity']
            print(f"  Grade {r['grade']}: Bed capacity ({r['bed_capacity']}) < Customers ({r['customers']})")
            print(f"           → {shortage} customers may leave due to long wait")

if __name__ == "__main__":
    main()
