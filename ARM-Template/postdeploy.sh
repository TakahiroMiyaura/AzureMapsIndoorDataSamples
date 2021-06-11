adtname=$1
egname=$2
egid=$3
funcappid=$4

echo "adt name: ${adtname}"
echo "egname: ${egname}"
echo "egid: ${egid}"
echo "funcappid: ${funcappid}"

# echo 'installing azure cli extension'
az config set extension.use_dynamic_install=yes_without_prompt

# az eventgrid topic create -g $rgname --name $egname -l $location
az dt endpoint create eventgrid --dt-name $adtname --eventgrid-resource-group $rgname --eventgrid-topic $egname --endpoint-name "$egname-ep"
az dt route create --dt-name $adtname --endpoint-name "$egname-ep" --route-name "$egname-rt"

# Create Subscriptions
az eventgrid event-subscription create --name "$egname-updatefeaturestate-sub" --source-resource-id $egid --endpoint "$funcappid/functions/updatemapsfeaturestate" --endpoint-type azurefunction