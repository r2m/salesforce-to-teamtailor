using System.Text.Json.Nodes;

namespace Magello
{
    public static class Mappings
    {
        public static readonly string SfRefTagPrefix = "sfref:";

        public static JsonNode SalesForceToTeamTailor(SalesForceJob sfJob)
        {
            var ttJob = new JsonObject();
            if (sfJob.TeamTailorUserId == null)
                return ttJob;

            var data = new JsonObject
            {
                ["type"] = "jobs"
            };

            var now = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK");
            var attributes = new JsonObject
            {
                ["title"] = sfJob.Name,
                ["body"] = Utils.TemplateTeamTailorBody(sfJob),
                ["picture"] = Utils.GetRandomPictureUrl(),
                ["status"] = "draft",
                ["created-at"] = now,
                ["updated-at"] = now,
                ["resume_requirement"] = "required",
                ["cover-letter-requirement"] = "off",
                ["apply-button-text"] = "ansök här",
                ["tags"] = new JsonArray(
                    "salesforce",
                    $"{sfJob.InternalRefNr}"
                )
            };

            var userData = new JsonObject
            {
                ["type"] = "users",
                ["id"] = int.Parse(sfJob.TeamTailorUserId.Replace(" ", ""))
            };

            var user = new JsonObject
            {
                ["data"] = userData
            };

            // 1136518 is 'Vilket är ditt prisförslag'
            // 1136520 is 'Ange konsultens tillgänglighet'
            var questionData = new JsonObject
            {
                ["data"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["id"] = "1136518",
                        ["type"] = "questions"
                    },
                    new JsonObject
                    {
                        ["id"] = "1136520",
                        ["type"] = "questions"
                    }
                }
            };

            // The picked question ids have been taken from the template job
            var pickedQuestions = new JsonObject
            {
                ["data"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["id"] = "14035840",
                        ["type"] = "picked-questions"
                    },
                    new JsonObject
                    {
                        ["id"] = "14035841",
                        ["type"] = "picked-questions"
                    }
                }
            };

            // The stages ids has also been taken from the template job
            var stages = new JsonObject
            {
                ["data"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["id"] = "15389830",
                        ["type"] = "stages"
                    },
                    new JsonObject
                    {
                        ["id"] = "15389831",
                        ["type"] = "stages"
                    },
                    new JsonObject
                    {
                        ["id"] = "15389832",
                        ["type"] = "stages"
                    },

                    new JsonObject
                    {
                        ["id"] = "15389834",
                        ["type"] = "stages"
                    },
                    new JsonObject
                    {
                        ["id"] = "15699653",
                        ["type"] = "stages"
                    }
                }
            };

            var relationships = new JsonObject
            {
                ["user"] = user,
                ["questions"] = questionData,
                ["picked-questions"] = pickedQuestions,
                ["stages"] = stages
            };

            data["attributes"] = attributes;
            data["relationships"] = relationships;
            ttJob["data"] = data;

            return ttJob;
        }

        public static JsonNode? CreateCustomFieldValues(
            SalesForceJob? sfJob,
            JsonNode? ttJob)
        {
            var fieldValues = new JsonObject();

            if (sfJob == null || ttJob == null)
                return null;

            // Get the id of the newly created job in team tailor
            var ttJobId = ttJob["data"]["id"].GetValue<string>();

            // Add data root object
            var data = new JsonObject();
            data["type"] = "custom-field-values";

            // Add custom field value to object
            var attributes = new JsonObject();
            attributes["value"] = sfJob.Id;
            data["attributes"] = attributes;

            var relationships = new JsonObject();
            var customField = new JsonObject();
            var customFieldData = new JsonObject();
            var owner = new JsonObject();
            var ownerData = new JsonObject();

            // Add custom field relationship
            customFieldData["id"] = int.Parse(Envs.GetEnvVar(Envs.E_SalesForceCustomFieldId));
            customFieldData["type"] = "custom-fields";
            customField["data"] = customFieldData;
            relationships["custom-field"] = customField;

            // Add owner relationship
            ownerData["type"] = "jobs";
            ownerData["id"] = int.Parse(ttJobId);
            owner["data"] = ownerData;
            relationships["owner"] = owner;

            // Add relationships to root
            data["relationships"] = relationships;

            fieldValues["data"] = data;

            return fieldValues;
        }
    }
}