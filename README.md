[![Build Status](https://travis-ci.org/alfredherr/Demo.svg?branch=master)](https://travis-ci.org/alfredherr/Demo)
[![Gitter chat](https://badges.gitter.im/ixtlesoft/Demo.png)](https://gitter.im/ixtlesoft/Demo) 
[![Waffle.io - Columns and their card count](https://badge.waffle.io/alfredherr/Demo.svg?columns=all)](http://waffle.io/alfredherr/Demo)

# PoC of Akka.Net on Docker
This is a POC of Akka.net on .Net Core deployed on Docker.  

The goals of this POC are as follows:
+ Determine viability of modeling a financial servicing domain using actors to hold state instead of using a database.
+ Determine the viability of using Akka.Net in a production-like environment.
+ Identify challenges of using Event Sourcing to model state transitions.

## Wanna help? 
Here is the list of pending [TODOs](TODO.md)

## High-level Objective
![alt text](https://github.com/alfredherr/Demo/blob/master/Akka%20POC.png "Diagram")


## Running It
```bash
cd AkkaNetPoC/ 
dotnet run --project Loaner
```
## To run it in a cluster use Lighthouse
```bash
cd AkkaNetPoC/ 
dotnet run --project Lighthouse.NetCoreApp
 ```

## Setting up Monitoring (Locally)
```bash
docker run -d --name=grafana  --restart=always -p 3000:3000 -e "GF_SERVER_ROOT_URL=http://localhost" -e "GF_SECURITY_ADMIN_PASSWORD=secret"  grafana/grafana
docker run -d --name=graphite --restart=always -p 80:80 -p 2003-2004:2003-2004 -p 2023-2024:2023-2024 -p 8125:8125/udp -p 8126:8126 hopsoft/graphite-statsd
```
or just run:
```bash
runMonitoring.sh
```


# To load new accounts
POST to this endpoint: [http://localhost:5000/api/system/simulation](http://localhost:5000/api/system/simulation)

Here is the model (note the path is relative to the source directory ‘Loaner’): 
```json
{
	"ClientName": "Raintree",
	"ClientAccountsFilePath": "./SampleData/Raintree.txt",
	"ObligationsFilePath":"./SampleData/Obligations/Raintree.txt"
}
```

Use the perl script to generate new accounts 
```shell
$> GenerateSampleData.pl <Total_#_of_Records> <#_of_Records_Per_Portfolio>
```

# To get an undecorated list of endpoints on the service
Simply open (GET) the root url: [http://localhost:5000](http://localhost:5000)

You will see the following: 

<body>
        <h1>Available routes:</h1>
        <table>
            <thead>
                <tr>
                    <th>Method</th>
                    <th>URL</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>GET</td>
                    <td>/api/account/{actorName}</td>
                </tr>
                <tr>
                    <td>GET</td>
                    <td>/api/account/{actorName}/info</td>
                </tr>
                <tr>
                    <td>GET</td>
                    <td>/api/account/{actorName}/assessment</td>
                </tr>
                <tr>
                    <td>POST</td>
                    <td>/api/account/{actorName}/assessment</td>
                </tr>
                <tr>
                    <td>GET</td>
                    <td>/</td>
                </tr>
                <tr>
                    <td>GET</td>
                    <td>/api/portfolio/{portfolioName}</td>
                </tr>
                <tr>
                    <td>GET</td>
                    <td>/api/portfolio/{portfolioName}/run</td>
                </tr>
                <tr>
                    <td>POST</td>
                    <td>/api/portfolio/{portfolioName}/assessment</td>
                </tr>
                <tr>
                    <td>GET</td>
                    <td>/api/system</td>
                </tr>
                <tr>
                    <td>GET</td>
                    <td>/api/system/run</td>
                </tr>
                <tr>
                    <td>GET</td>
                    <td>/api/system/businessrules</td>
                </tr>
                <tr>
                    <td>POST</td>
                    <td>/api/system/businessrules</td>
                </tr>
                <tr>
                    <td>POST</td>
                    <td>/api/system/billall</td>
                </tr>
                <tr>
                    <td>GET</td>
                    <td>/api/system/BillingStatus</td>
                </tr>
                <tr>
                    <td>POST</td>
                    <td>/api/system/simulation</td>
                </tr>
            </tbody>
        </table>
    </body>

# To load/run all the actors
GET the following endpoint: [http://localhost:5000/api/system/run](http://localhost:5000/api/system/run)

This is also how you can get the list of all portfolios, the model looks like this: 
```JSOn
{
    "message": "5 portfolios started.",
    "portfolios": {
        "portfolioUPD": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD#983089506]",
        "portfolioACE": "[akka://demoSystem/user/demoSupervisor/PortfolioACE#220375013]",
        "portfolioMGO": "[akka://demoSystem/user/demoSupervisor/PortfolioMGO#1590969739]",
        "portfolioDHY": "[akka://demoSystem/user/demoSupervisor/PortfolioDHY#1239442796]",
        "portfolioISR": "[akka://demoSystem/user/demoSupervisor/PortfolioISR#1050964355]"
    }
}
```

# To get a list of accounts in a portfolio 
GET this endpoint: [http://localhost:5000/api/portfolio/PortfolioUPD/](http://localhost:5000/api/portfolio/PortfolioUPD/)

This is what you’ll see ( I think I limit to 10,000): 
```JSOn
{
    "message": "18 accounts. I was last booted up on: 1/8/2018 8:13:48 PM",
    "accounts": {
        "55531527681": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/55531527681#1089436280]",
        "86193236762": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/86193236762#665658973]",
        "27365457578": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/27365457578#1659917487]",
        "21733172322": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/21733172322#1266566271]",
        "74586121145": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/74586121145#1151499661]",
        "79677466751": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/79677466751#506411013]",
        "98917429792": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/98917429792#1372229082]",
        "55765352539": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/55765352539#1971395320]",
        "81914734116": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/81914734116#1111536982]",
        "86386157956": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/86386157956#1957069331]",
        "58331699252": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/58331699252#880605936]",
        "11692231568": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/11692231568#216994696]",
        "58138366935": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/58138366935#1250894360]",
        "76751575336": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/76751575336#481362170]",
        "51947728595": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/51947728595#2020103961]",
        "61562384728": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/61562384728#696398680]",
        "15394947113": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/15394947113#24798788]",
        "98247821733": "[akka://demoSystem/user/demoSupervisor/PortfolioUPD/98247821733#486265728]"
    }
}
```

# To get a list of possible commands to run for billing and the list of possible business rules 
GET to this endpint: [http://localhost:5000/api/system/businessrules](http://localhost:5000/api/system/businessrules)

Parameters are key/value pairs. If the rule has parameters, they are listed with the key name and they datatype/description of accetable values to set. : 
```json
[
    {
        "command": "BillingAssessment",
        "businessRule": "AccountBalanceMustNotBeNegative",
        "parameters": {},
        "description": "Fail creating the billing if the account balance is negative."
    },
    {
        "command": "BillingAssessment",
        "businessRule": "AnObligationMustBeActiveForBilling",
        "parameters": {},
        "description": "Fail creating the billing if the account doesn't have an active obligation."
    },
    {
        "command": "BillingAssessment",
        "businessRule": "AssessTaxAsPercentageOfDuesDuringBilling",
        "parameters": {
            "taxPercentageRate": "[Double]"
        },
        "description": "Assess a tax concept to billing based on the amount of Dues."
    },
    {
        "command": "BillingAssessment",
        "businessRule": "ApplyUacAfterBilling",
        "parameters": {
            "minimumAmount": "[Double]",
            "maxAmount": "[Double]"
        },
        "description": "If there is Unapplied Cash (i.e account balance is negative), apply it immediately after billing. A minimum or maximum can be set. Use 0 for no minimum and 999999 for no maximum. "
    },
    {
        "command": "BillingAssessment",
        "businessRule": "BillingConceptCannotBeBilledMoreThanOnce",
        "parameters": {},
        "description": "Fail creating the billing if the account has been assessed a billing for the same concept."
    },
    {
        "command": "BillingAssessment",
        "businessRule": "ClientSpecificRuleForCalculatingTax",
        "parameters": {
            "excludedStates": "[StateAbbreviation/OnePerState]",
            "californiaTaxPercentageRate": "[Double]"
        },
        "description": "Client specific rule for Client ThousandTrails. Some states do not get assessed tax; others do, rates vary. Here maybe i reference a URL where more details are available about this rule."
    }
]
```
# To create associations between business rules and accounts/portfolios/clients
POST to the following endpoint: [http://localhost:5000/api/system/businessrules](http://localhost:5000/api/system/businessrules)

Note that parameters mus tbe provided as single string, like so: "MinimumAmount=1.00,MaxAmount=999999". 

Use "NoParameters" if there are none.

```json
[
    {
        "client": "ClientRaintree",
        "portfolio": "PortfolioUPD",
        "accountNumber": "*",
        "forAllAccounts": true,
        "command": "BillingAssessment",
        "businessRule": "AnObligationMustBeActiveForBilling",
        "businessRuleParameters": "NoParameters"
    },
    {
        "client": "ClientRaintree",
        "portfolio": "PortfolioUPD",
        "accountNumber": "*",
        "forAllAccounts": true,
        "command": "BillingAssessment",
        "businessRule": "AccountBalanceMustNotBeNegative",
        "businessRuleParameters": "NoParameters"
    },
    {
        "client": "ClientRaintree",
        "portfolio": "PortfolioISR",
        "accountNumber": "*",
        "forAllAccounts": true,
        "command": "BillingAssessment",
        "businessRule": "AssessTaxAsPercentageOfDuesDuringBilling",
        "businessRuleParameters": "TaxPercentageRate=8.9"
    },
    {
        "client": "ClientRaintree",
        "portfolio": "PortfolioMGO",
        "accountNumber": "*",
        "forAllAccounts": true,
        "command": "BillingAssessment",
        "businessRule": "ApplyUacAfterBilling",
        "businessRuleParameters": "MinimumAmount=1.00,MaxAmount=999999"
    },
    {
        "client": "ClientRaintree",
        "portfolio": "PortfolioMGO",
        "accountNumber": "*",
        "forAllAccounts": true,
        "command": "BillingAssessment",
        "businessRule": "ClientSpecificRuleForCalculatingTax",
        "businessRuleParameters": "ExcludedStates=AZ,CaliforniaTaxPercentageRate=9.9"
    },
    {
        "client": "ClientRaintree",
        "portfolio": "PortfolioISR",
        "accountNumber": "*",
        "forAllAccounts": true,
        "command": "BillingAssessment",
        "businessRule": "BillingConceptCannotBeBilledMoreThanOnce",
        "businessRuleParameters": "NoParameters"
    }
]
```
Be aware that I’m just taking what you post and removing any previous rule associations. It’s very basic at the moment.

# To trigger billing
POST to the following endpoint: [http://localhost:5000/api/portfolio/PortfolioACE/assessment](http://localhost:5000/api/portfolio/PortfolioACE/assessment
)
Using the following model (you can bill one or more ‘items’, currently these are all possible types): 
```json
[
    {
        "item": {
            "name": "Tax",
            "amount": 10
        }
    },
    {
        "item": {
            "name": "Dues",
            "amount": 100
        }
    },
    {
        "item": {
            "name": "Reserve",
            "amount": 25
        }
    }
]
```
