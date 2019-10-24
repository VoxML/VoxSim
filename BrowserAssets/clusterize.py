from clustergrammer import Network
net = Network()

# load matrix file
net.load_file('txt/new.txt')

# calculate clustering
net.make_clust(dist_type='cos',views=['N_row_sum', 'N_row_var'])

# write visualization json to file
net.write_json_to_file('viz', 'clustergrammer-master/json/new_clust.json')