BeforeAll {
    $script:Script = Join-Path $PSScriptRoot 'Assert-VersionIsNewer.ps1'
}

Describe 'Assert-VersionIsNewer' {
    Context 'As a workflow run checking a change, when the latest release is 5.0.5' {
        BeforeAll {
            $script:Released = @('5.0.3', '5.0.4', '5.0.5')
        }

        It 'Should pass a <Bump> bump to <Version>' -TestCases @(
            @{ Bump = 'patch'; Version = '5.0.6' }
            @{ Bump = 'minor'; Version = '5.1.0' }
            @{ Bump = 'major'; Version = '6.0.0' }
        ) {
            { & $Script -Version $Version -ReleasedVersions $Released } | Should -Not -Throw
        }

        It 'Should fail a version that was not bumped, naming both versions' {
            $failure = { & $Script -Version '5.0.5' -ReleasedVersions $Released } | Should -Throw -PassThru

            $failure.Exception.Message | Should -Be 'Version 5.0.5 must be greater than the highest released version, 5.0.5.'
        }

        It 'Should fail a version lower than the latest release, naming both versions' {
            $failure = { & $Script -Version '5.0.4' -ReleasedVersions $Released } | Should -Throw -PassThru

            $failure.Exception.Message | Should -Be 'Version 5.0.4 must be greater than the highest released version, 5.0.5.'
        }
    }

    Context 'As a workflow run checking a change, when versions differ in digit count' {
        It 'Should pass 5.0.10 after 5.0.9, comparing as versions, not text' {
            { & $Script -Version '5.0.10' -ReleasedVersions @('5.0.8', '5.0.9') } | Should -Not -Throw
        }
    }

    Context 'As a workflow run checking a change, when a release is tagged with a leading v' {
        It 'Should count v3.0.0 as 3.0.0' {
            $failure = { & $Script -Version '3.0.0' -ReleasedVersions @('2.0.1', 'v3.0.0') } | Should -Throw -PassThru

            $failure.Exception.Message | Should -Be 'Version 3.0.0 must be greater than the highest released version, 3.0.0.'
        }
    }

    Context 'As a workflow run checking a change, when releases are listed out of order' {
        It 'Should compare against the highest release, not the first or last listed' {
            $failure = { & $Script -Version '5.0.5' -ReleasedVersions @('4.1.5', '5.0.5', '1.0.0', 'v3.0.0', '5.0.0') } | Should -Throw -PassThru

            $failure.Exception.Message | Should -Be 'Version 5.0.5 must be greater than the highest released version, 5.0.5.'
        }
    }

    Context 'As a workflow run checking the first change of a new repository, with no releases' {
        It 'Should pass any version' {
            { & $Script -Version '1.0.0' -ReleasedVersions @() } | Should -Not -Throw
        }
    }
}
