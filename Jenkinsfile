pipeline {
    agent any
    stages {
        stage('Checkout') {
            steps {
                echo 'Checkout...'
                checkout scm
            }
        }
        stage('Restore the project') {
            steps {
                echo 'Restore the project...'
                sh 'dotnet restore'
            }
        }
        stage('Build the project') {
            steps {
                echo 'Build the project'
                sh 'dotnet build --no-restore'
            }
        }
        stage('Test the project') {
            steps {
                echo 'Test the project'
                sh 'dotnet test --no-build --verbosity normal'
            }
        }
    }
}