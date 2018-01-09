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
```
    Available routes:
    Method	URL
    GET	/api/account/{actorName}
    GET	/api/account/{actorName}/info
    GET	/api/account/{actorName}/assessment
    POST	/api/account/{actorName}/assessment
    GET	/
    GET	/api/portfolio/{portfolioName}
    GET	/api/portfolio/{portfolioName}/run
    POST	/api/portfolio/{portfolioName}/assessment
    GET	/api/system
    GET	/api/system/run
    GET	/api/system/businessrules
    POST	/api/system/businessrules
    POST	/api/system/billall
    GET	/api/system/BillingStatus
    POST	/api/system/simulation
```
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

You will see this model (in this case you can see AnObligationMustBeActiveForBilling & AccountBalanceMustNotBeNegative are available for the BillingAssessment command) : 
```JSON
[
    {
        "command": "BillingAssessment",
        "businessRule": "AnObligationMustBeActiveForBilling"
    },
    {
        "command": "BillingAssessment",
        "businessRule": "AccountBalanceMustNotBeNegative"
    }
]
```
# To create associations between business rules and accounts/portfolios/clients
POST to the following endpoint: [http://localhost:5000/api/system/businessrules](http://localhost:5000/api/system/businessrules)

using this model:
```JSOn
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
    }
]
```
Be aware that I’m just taking what you post and removing any previous rule associations. It’s very basic at the moment.

# To trigger billing
POST to the following endpoint: [http://localhost:5000/api/portfolio/PortfolioACE/assessment](http://localhost:5000/api/portfolio/PortfolioACE/assessment
)
Using the following model (you can bill one or more ‘items’, currently these are all possible types): 
```JSOn
[
    {
        "item": {
            "name": "Tax",
            "amount": 10
        },
        "units": 1,
        "unitAmount": 1,
        "totalAmount": 10
    },
    {
        "item": {
            "name": "Dues",
            "amount": 100
        },
        "units": 1,
        "unitAmount": 100,
        "totalAmount": 100
    },
    {
        "item": {
            "name": "Reserve",
            "amount": 25
        },
        "units": 1,
        "unitAmount": 25,
        "totalAmount": 25
    }
]
```
