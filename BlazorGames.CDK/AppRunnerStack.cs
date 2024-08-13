using Amazon.CDK;
using Amazon.CDK.AWS.AppRunner.Alpha;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace BlazorGames.CDK;

public class AppRunnerStackProps : StackProps
{
    public IVpc Vpc { get; set; }
}

public class AppRunnerStack : Stack
{
    private SecurityGroup appRunnerSecurityGroup;
    private Role appRunnerInstanceRole;
    private Role appRunnerEcrRole;
    private DockerImageAsset dockerImage;

    internal AppRunnerStack(Construct scope, string id, AppRunnerStackProps props) : base(scope, id, props)
    {
        CreateAppRunnerSecurityGroup(props);

        CreateAppRunnerInstanceRole(props);

        CreateAppRunnerEcrRole();

        CreateDockerImage();

        var appRunnerService = CreateAppRunnerService(props);

    }

    internal void CreateAppRunnerSecurityGroup(AppRunnerStackProps props)
    {
        appRunnerSecurityGroup = new SecurityGroup(this, "AppRunnerSecurityGroup", new SecurityGroupProps
        {
            SecurityGroupName = $"{Constants.AppName}AppRunnerSecurityGroup",
            Vpc = props.Vpc,
            AllowAllOutbound = true
        });
    }

    internal void CreateAppRunnerInstanceRole(AppRunnerStackProps props)
    {
        appRunnerInstanceRole = new Role(this, "AppRunnerRole", new RoleProps
        {
            AssumedBy = new ServicePrincipal("tasks.apprunner.amazonaws.com")
        });

        // Access to read parameters by path is not in the AmazonSSMManagedInstanceCore
        // managed policy
        appRunnerInstanceRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = new[] { "ssm:GetParametersByPath" },
            Resources = new[]
            {
                Arn.Format(new ArnComponents
                {
                    Service = "ssm",
                    Resource = "parameter",
                    ResourceName = $"{Constants.AppName}/*"
                }, this)
            }
        }));

        // Add permissions to write logs
        appRunnerInstanceRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = new[]
            {
                "logs:DescribeLogGroups",
                "logs:CreateLogGroup",
                "logs:CreateLogStream",
                "logs:PutLogEvents"
            },
            Resources = new[]
            {
                "arn:aws:logs:*:*:log-group:*:log-stream:*"
            }
        }));

    }

    internal void CreateAppRunnerEcrRole()
    {
        appRunnerEcrRole = new Role(this, "AppRunnerEcrRole", new RoleProps
        {
            AssumedBy = new ServicePrincipal("build.apprunner.amazonaws.com"),
            RoleName = $"{Constants.AppName}AppRunnerEcrRole"
        });
    }

    internal void CreateDockerImage()
    {
        dockerImage = new DockerImageAsset(this, $"{Constants.AppName}AppRunnerDockerImage", new DockerImageAssetProps
        {
            Directory = "" //This points to the current directory
        });
    }

    private Service CreateAppRunnerService(AppRunnerStackProps props)
    {
        var appRunnerService = new Service(this, "AppRunnerService", new ServiceProps
        {
            Source = Source.FromAsset(new AssetProps { Asset = dockerImage, ImageConfiguration = new ImageConfiguration { Port = 80 } }),
            Cpu = Cpu.HALF_VCPU,
            Memory = Memory.ONE_GB,
            InstanceRole = appRunnerInstanceRole,
            AccessRole = appRunnerEcrRole, 
            VpcConnector = new VpcConnector(this, "VPCConnector", new VpcConnectorProps
            {
                VpcConnectorName = $"{Constants.AppName}VPCConnector",
                Vpc = props.Vpc,
                VpcSubnets = new SubnetSelection { Subnets = props.Vpc.PrivateSubnets },
                SecurityGroups = new[] { appRunnerSecurityGroup }
            })
        });

        // output apprunner url
        _ = new CfnOutput(this, "AppRunnerUrl", new CfnOutputProps
        {
            Value = $"https://{appRunnerService.ServiceUrl}"
        });

        return appRunnerService;
    }
    
}