# salesforce-to-teamtailor
Azure function middleware to handle API-to-API communication between SalesForce and TeamTailor

# Salesforce
* Flow
    * Low-code tool for process generation
* Invocation-class
    * Class with custom @annotations for flow-usage (input, output..)
* API-class
    * Code that does the actual call to the API middleware
* Metadata
    * Table with mappings between logins in SF and userids in TT
* Settings
    * String value that holds the url and token key for the Azure Function running the middleware

Button -> Flow -> Invocation -> API-class -> Metadata -> Settings -> Call API

API docs: https://developer.salesforce.com/docs/atlas.en-us.api_rest.meta/api_rest/quickstart_oauth.htm

# Middleware
* JSON -> POJO mapping
* POJO -> TT/SF field mapping
* TeamTailor API token
* Salesforce API token
* Business logic

# TeamTailor
