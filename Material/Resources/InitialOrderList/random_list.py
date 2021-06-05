import random
import csv

if __name__ == '__main__':
    list1 = list(range(1, 46))
    list2 = list(range(46,91))
    list3 = list(range(91,135))
    random.shuffle(list1)
    random.shuffle(list2)
    random.shuffle(list3)

    f = open('random_list_3.csv','w', newline='')
    wr = csv.writer(f)
    for i in range(45):
        try:
            random_list = []
            random_list.append(list1[i])
            random_list.append(list2[i])
            random_list.append(list3[i])
        except IndexError:
            pass
        wr.writerow(random_list)