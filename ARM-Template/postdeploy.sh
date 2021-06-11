adtname=$1
rgname=$2
egname=$3
egid=$4
funcappid=$5

echo "adt name: ${adtname}"
echo "rgname:" ${rgname}
echo "egname: ${egname}"
echo "egid: ${egid}"
echo "funcappid: ${funcappid}"

# echo 'installing azure cli extension'
az config set extension.use_dynamic_install=yes_without_prompt

# az eventgrid topic create -g $rgname --name $egname -l $location
az dt endpoint create eventgrid --dt-name $adtname --eventgrid-resource-group $rgname --eventgrid-topic $egname --endpoint-name "$egname-ep"
az dt route create --dt-name $adtname --endpoint-name "$egname-ep" --route-name "$egname-rt"

# Create Subscriptions
az eventgrid event-subscription create --name "$egname-ufstate-sub" --source-resource-id $egid --endpoint "$funcappid/functions/updatemapsfeaturestate" --endpoint-type azurefunction