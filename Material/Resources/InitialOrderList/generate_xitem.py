import csv
import numpy as np
# sku, bundle size, Time(second)
# when bundle is made, number of orders
ID = range(1,135)
Size = [4, 8, 12, 16]
Time = 288000
interval_Bundle = 30
Limit = 10000

# Read Csv File
orders = []
with open("pick_order.csv", "r", encoding='UTF8') as f:
    order_list = csv.reader(f)
    for i, _order in enumerate(order_list):
        orders.append(_order)
        orders[i] = list(filter(None, orders[i]))

np.random.shuffle(orders)
for i, _order in enumerate(orders):
    orders[i] = list(map(int, _order))
    
# print(orders[3])

#--------------------------------------------------------------#
file_name = "Generate_orderlines_" + str(Limit) + ".txt"
xitem_file = open(file_name, 'w')
xitem_file.write('<?xml version="1.0" encoding="utf-8"?>\n')
xitem_file.write('<OrderList xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" Type="Letter">\n')

# Write ItemDescription 
xitem_file.write("  <ItemDescriptions>\n")
for i in ID:
    xitem_file.write('    <ItemDescription ID="{}" Type="SimpleItem" Weight="2"/>\n'.format(i))
xitem_file.write("  <ItemDescriptions>\n")

# Write ItemBundle
xitem_file.write("  <ItemBundles>\n")

# Generate ItemBundle TimeStamp
total_item_number = np.zeros(134)
temp_item_number = np.zeros(134)
current_time = 0
interval = float(Time / Limit)
selected_item = []

# randomly set interval of itembundle's timestamp
for i,one_order in enumerate(orders):
    for sku in range(1,135):
        total_item_number[sku-1] += one_order.count(sku)
    
for sku in range(1,135):        
    total_item_number[sku-1] *= 1.1

for i, one_order in enumerate(orders):
    # go when interval of number of orders is 0~29
    if (i%interval_Bundle != 0 or i == 0):
        for sku in range(1,135):
            # Every number of item is saved at temp_item_number
            temp_item_number[sku-1] += one_order.count(sku)
            # 조건을 만족하는 sku list
            # randomly set the boundary and select one item to bring in itembundle list
            if temp_item_number[sku-1] >7: #(abs(total_item_number[sku-1] - temp_item_number[sku-1]) < 0.99*total_item_number[sku-1]):
                selected_item.append(sku)

    elif (i%interval_Bundle == 0):
        one_item = np.random.choice(selected_item, 1)
        item_timestamp = np.random.uniform(current_time, current_time + interval,1)
        size = temp_item_number[one_item-1]
        # write a line of itembundle
        xitem_file.write('    <ItemBundle TimeStamp="{}" ItemDescription="{}" Size="{}" />\n'.format(float(item_timestamp), int(one_item), int(size)))
        current_time = interval*i
        # Initialize
        temp_item_number = np.zeros(134)
        selected_item = []

        for sku in range(1,135):
            # Every number of item is saved at temp_item_number
            temp_item_number[sku-1] += one_order.count(sku)
            # 조건을 만족하는 sku list
            # randomly set the boundary and select one item to bring in itembundle list
            if temp_item_number[sku-1] >7: #(abs(total_item_number[sku-1] - temp_item_number[sku-1]) < 0.98*total_item_number[sku-1]):
                selected_item.append(sku)
        
    if i == Limit:
        break

xitem_file.write("  <ItemBundles>\n")

# Generate Order TimeStamp and Write Order
xitem_file.write("  <Orders>\n")
current_time = 0
interval = float(Time / Limit)
print(interval)
for i, one_order in enumerate(orders):
    order_timestamp = np.random.uniform(current_time, current_time + interval,1)
    xitem_file.write('    <Order TimeStamp="{}">\n'.format(float(order_timestamp)))
    xitem_file.write("      <Positions>\n")
    current_time = interval*i

    count_each_value = {}
    for sku in one_order:
        try: count_each_value[sku] += 1
        except: count_each_value[sku] = 1
    
    for key, value in count_each_value.items():
        xitem_file.write('<Position ItemDescriptionID="{}" Count="{}" />\n'.format(key, value))
    xitem_file.write("      <Positions>\n")
    xitem_file.write("    </Order>\n")

    if i == Limit:
        break


xitem_file.write("  <Orders>\n")
xitem_file.write("</OrderList>\n")
xitem_file.close()
