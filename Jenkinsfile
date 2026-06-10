pipeline {
    agent any

    options {
        skipDefaultCheckout(true)
    }

    environment {
        CONTAINER_NAME  = 'pharmeasy-backend'
        IMAGE_NAME      = 'pharmeasy-backend'
        NETWORK_NAME    = 'pharmeasy-network'
        DB_CONTAINER    = 'pharmeasy-mysql'
        PORT_MAPPING    = '8081:8080'
        DOCKER_BUILDKIT = '0'
    }

    stages {

        stage('Checkout') {
            steps {
                // Use Windows native SSL stack (SChannel) so Git inherits the
                // system proxy and trusts the Sophos CA cert automatically.
                bat 'git config --global http.sslBackend schannel'
                bat 'git config --global --unset http.proxy  || ver >nul'
                bat 'git config --global --unset https.proxy || ver >nul'
                checkout scm
            }
        }

        stage('Ensure MySQL Running') {
            steps {
                script {
                    bat "docker network create ${NETWORK_NAME} 2>nul || ver >nul"

                    def mysqlExitCode = bat(
                        script: "docker inspect -f {{.State.Running}} ${DB_CONTAINER}",
                        returnStatus: true
                    )

                    if (mysqlExitCode != 0) {
                        echo "MySQL container not found — starting it now."
                        bat "docker rm ${DB_CONTAINER} 2>nul || ver >nul"
                        bat """
                            docker run -d ^
                                --name ${DB_CONTAINER} ^
                                --network ${NETWORK_NAME} ^
                                -p 3307:3306 ^
                                --restart unless-stopped ^
                                -e MYSQL_ROOT_PASSWORD=root ^
                                -e MYSQL_DATABASE=pharmeasy ^
                                -v pharmeasy-mysql-data:/var/lib/mysql ^
                                mysql:8.0
                        """
                        echo "Waiting for MySQL to be ready..."
                        sleep(time: 20, unit: 'SECONDS')
                    } else {
                        echo "MySQL container is already running."
                    }
                }
            }
        }

        stage('Build Docker Image') {
            steps {
                bat "docker build --no-cache -t ${IMAGE_NAME}:latest -t ${IMAGE_NAME}:${BUILD_NUMBER} ."
            }
        }

        stage('Deploy Container') {
            steps {
                script {
                    bat "docker stop ${CONTAINER_NAME} 2>nul || ver >nul"
                    bat "docker rm   ${CONTAINER_NAME} 2>nul || ver >nul"

                    bat """
                        docker run -d ^
                            --name ${CONTAINER_NAME} ^
                            --network ${NETWORK_NAME} ^
                            -p ${PORT_MAPPING} ^
                            --restart unless-stopped ^
                            -e ASPNETCORE_URLS=http://+:8080 ^
                            -e "ConnectionStrings__DefaultConnection=Server=pharmeasy-mysql;Port=3306;Database=pharmeasy;User=root;Password=root;" ^
                            -e "Database__Provider=MySql" ^
                            -e "Database__ConnectionString=Server=pharmeasy-mysql;Port=3306;Database=pharmeasy;User=root;Password=root;" ^
                            -e "Jwt__SecretKey=masaiseceret_pharmeasy_jwt_secret_key_2024" ^
                            -e "Smtp__Host=smtp.gmail.com" ^
                            -e "Smtp__Port=587" ^
                            -e "Smtp__Username=adheenalnest@gmail.com" ^
                            -e "Smtp__Password=swvt papw wcou lois" ^
                            -e "Smtp__From=adheenalnest@gmail.com" ^
                            -e "Razorpay__KeyId=rzp_test_Syl5owGtFUehSW" ^
                            -e "Razorpay__KeySecret=05dw8wKk5XjjkLYJfz9lQ5eG" ^
                            -e "Razorpay__Currency=INR" ^
                            -e "Razorpay__CallbackUrl=http://localhost:4201/order-success" ^
                            -e "Razorpay__BookingCallbackUrl=http://localhost:4201/booking-success" ^
                            ${IMAGE_NAME}:latest
                    """
                }
            }
        }

        stage('Health Check') {
            steps {
                script {
                    sleep(time: 10, unit: 'SECONDS')
                    def exitCode = bat(
                        script: "docker inspect -f {{.State.Running}} ${CONTAINER_NAME}",
                        returnStatus: true
                    )
                    if (exitCode != 0) {
                        error "Container ${CONTAINER_NAME} failed to start. Check: docker logs ${CONTAINER_NAME}"
                    }
                    echo "Backend container is healthy and running on port 8081."
                }
            }
        }
    }

    post {
        success {
            echo "PharmEasy backend pipeline completed successfully! API is live at http://localhost:8081"
        }
        failure {
            echo "PharmEasy backend pipeline failed. Check the logs above for details."
            script {
                try {
                    bat "docker logs ${env.CONTAINER_NAME} 2>nul || ver >nul"
                } catch (Exception e) {
                    echo "Could not fetch container logs (container may not have started): ${e.message}"
                }
            }
        }
        always {
            script {
                try {
                    bat "docker image prune -f 2>nul || ver >nul"
                } catch (Exception e) {
                    echo "Could not prune images: ${e.message}"
                }
            }
        }
    }
}
