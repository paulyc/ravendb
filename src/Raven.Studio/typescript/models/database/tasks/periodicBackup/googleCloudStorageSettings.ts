import backupSettings = require("models/database/tasks/periodicBackup/backupSettings");
import jsonUtil = require("common/jsonUtil");

class googleCloudStorageSettings extends backupSettings {
    bucket = ko.observable<string>();
    remoteFolderName = ko.observable<string>();
    GoogleCredentialsJson = ko.observable<string>();

    constructor(dto: Raven.Client.Documents.Operations.Backups.GoogleCloudStorageSettings) {
        super(dto, "GoogleCloudStorage");

        this.bucket(dto.BucketName);
        this.remoteFolderName(dto.RemoteFolderName);
        this.GoogleCredentialsJson(dto.GoogleCredentialsJson);

        this.initValidation();

        this.dirtyFlag = new ko.DirtyFlag([
            this.enabled,
            this.bucket,
            this.remoteFolderName,
            this.GoogleCredentialsJson
        ], false, jsonUtil.newLineNormalizingHashFunction);
    }

    initValidation() {
        const allowedCharactersRegExp = /^[a-z0-9.-_]+$/;
        const allowedBeginningCharactersRegExp = /^[a-z0-9]+$/;
        const firstDashRuleRegExp = /.-/;
        const secondDashRuleRegExp = /../;
        const thirdDashRuleRegExp = /-./;
        const fourthDashesRegExp = /_./;
        const fifthDashesRegExp = /._/;
        this.bucket.extend({
            validation: [
                {
                    validator: (bucket: string) => this.validate(() =>
                        bucket && bucket.length >= 3 && bucket.length <= 222),
                    message: "Bucket name must contain 3 to 63 characters." +
                        "Names containing dots can contain up to 222 characters, but each dot-separated component can be no longer than 63 characters"
                },
                {
                    validator: (bucket: string) => this.validate(() =>
                        allowedCharactersRegExp.test(bucket)),
                    message: "Bucket name must contain only lowercase letters, numbers, dashes (-), underscores (_), and dots (.)"
                },
                {
                    validator: (bucket: string) => this.validate(() =>
                        bucket && allowedBeginningCharactersRegExp.test(bucket[0]) && allowedBeginningCharactersRegExp.test(bucket[bucket.length - 1])),
                    message: "Bucket name must start and end with a number or letter"
                },
                {
                    validator: (bucket: string) => this.validate(() =>
                        !firstDashRuleRegExp.test(bucket) || !secondDashRuleRegExp.test(bucket) || !thirdDashRuleRegExp.test(bucket) ||
                        !fourthDashesRegExp.test(bucket) || !fifthDashesRegExp.test(bucket)),
                    message: "Dashes, periods and underscores are not permitted to be adjacent to another"
                }
            ]
        });

        this.GoogleCredentialsJson.extend({
            required: {
                onlyIf: () => this.enabled()
            },
            validation: [
                {
                    validator: (GoogleCredentialsJson: string) => this.validate(() =>
                    GoogleCredentialsJson.includes("\"type\"")),
                    message: "Google credentials json is missing 'type' field" 
                },
                {
                    validator: (GoogleCredentialsJson: string) => this.validate(() =>
                    GoogleCredentialsJson.includes("\"private_key\"")),
                    message: "Google credentials json is missing 'private_key' field" 
                },
                {
                    validator: (GoogleCredentialsJson: string) => this.validate(() =>
                    GoogleCredentialsJson.includes("\"client_email\"")),
                    message: "Google credentials json is missing 'client_email' field" 
                }
            ]
        });

        this.validationGroup = ko.validatedObservable({
            bucket: this.bucket,
            accountKey: this.GoogleCredentialsJson
        });
    }

    toDto(): Raven.Client.Documents.Operations.Backups.GoogleCloudStorageSettings {
        const dto = super.toDto() as Raven.Client.Documents.Operations.Backups.GoogleCloudStorageSettings;
        dto.BucketName = this.bucket();
        dto.RemoteFolderName = this.remoteFolderName();
        dto.GoogleCredentialsJson = this.GoogleCredentialsJson();
        return dto;
    }

    static empty(): googleCloudStorageSettings {
        return new googleCloudStorageSettings({
            Disabled: true,
            RemoteFolderName: null,
            GoogleCredentialsJson: null,
            BucketName: null
        });
    }
}

export = googleCloudStorageSettings;