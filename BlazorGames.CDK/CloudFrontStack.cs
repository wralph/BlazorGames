using Amazon.CDK;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Constructs;

namespace BlazorGames.CDK;

internal class CloudFrontStackProps : StackProps
{
    public string ApplicationPath { get; set; }
}

public class CloudFrontStack : Stack
{

    public IBucket ApplicationBucket { get; private set; }

    public CloudFrontWebDistribution GamesDistribution { get; set; }


    internal CloudFrontStack(Construct scope, string id, CloudFrontStackProps props = null) : base(scope, id, props)
    {
        
        CreateApplicationS3Bucket();
        CreateCloudFrontDistribution();
        UploadAssets(props);
    }


    internal void CreateApplicationS3Bucket()
    {
        ApplicationBucket = new Bucket(this, "S3Bucket", new BucketProps
        {
            // !DO NOT USE THESE TWO SETTINGS FOR PRODUCTION DEPLOYMENTS - YOU WILL LOSE DATA
            // WHEN THE STACK IS DELETED!
            AutoDeleteObjects = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            WebsiteIndexDocument = "index.html"
        });

    }

    internal void UploadAssets(CloudFrontStackProps props)
    {
        new BucketDeployment(this, "ApplicationDeployment", new BucketDeploymentProps
        {
            Sources = new[]
            {
                Source.Asset(props.ApplicationPath)
            },
            DestinationBucket = ApplicationBucket,
            Distribution = GamesDistribution
        });
    }

    internal void CreateCloudFrontDistribution()
    {
        //=========================================================================================
        // Access to the bucket is only granted to traffic coming from a CloudFront distribution
        //
        var cloudfrontOAI = new OriginAccessIdentity(this, "CloudFrontOriginAccessIdentity");

        var policyProps = new PolicyStatementProps
        {
            Actions = new[] { "s3:GetObject" },
            Resources = new[] { ApplicationBucket.ArnForObjects("*") },
            Principals = new[]
            {
                new CanonicalUserPrincipal
                (
                    cloudfrontOAI.CloudFrontOriginAccessIdentityS3CanonicalUserId
                )
            }
        };

        ApplicationBucket.AddToResourcePolicy(new PolicyStatement(policyProps));

        // Place a CloudFront distribution in front of the storage bucket. S3 will only respond to
        // requests for objects if that request came from the CloudFront distribution.
        var distProps = new CloudFrontWebDistributionProps
        {
            OriginConfigs = new[]
            {
                new SourceConfiguration
                {
                    S3OriginSource = new S3OriginConfig
                    {
                        S3BucketSource = ApplicationBucket,
                        OriginAccessIdentity = cloudfrontOAI
                    },
                    Behaviors = new []
                    {
                        new Behavior
                        {
                            IsDefaultBehavior = true,
                            Compress = true,
                            AllowedMethods = CloudFrontAllowedMethods.GET_HEAD_OPTIONS
                        }
                    }
                }
            },
            // Require HTTPS between viewer and CloudFront; CloudFront to
            // origin (the bucket) will use HTTP but could also be set to require HTTPS
            ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS
        };

        GamesDistribution = new CloudFrontWebDistribution(this, "CloudFrontDistribution", distProps);

        // output the distribution domain name
        new CfnOutput(this, "CloudFrontDomain", new CfnOutputProps
        {
            Value = $"https://{GamesDistribution.DistributionDomainName}"
        });
    }
}
