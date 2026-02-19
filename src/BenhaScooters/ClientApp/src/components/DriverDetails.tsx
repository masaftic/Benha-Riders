import { useState, useEffect } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { adminAPI, type DriverDetails as DriverDetailsType } from '../services/api'
import { AxiosError } from 'axios'

interface DriverDetailsProps {
  onLogout: () => void
}

interface InfoRowProps {
  label: string
  value: string | React.ReactNode
}

function DriverDetails({ onLogout }: DriverDetailsProps) {
  const { driverId } = useParams<{ driverId: string }>()
  const navigate = useNavigate()
  const [driver, setDriver] = useState<DriverDetailsType | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [actionLoading, setActionLoading] = useState(false)
  const [banReason, setBanReason] = useState('')
  const [showBanModal, setShowBanModal] = useState(false)

  useEffect(() => {
    if (driverId) {
      fetchDriverDetails()
    }
  }, [driverId])

  const fetchDriverDetails = async () => {
    setIsLoading(true)
    try {
      const response = await adminAPI.getDriverDetails(Number(driverId))
      setDriver(response.data)
    } catch (err) {
      setError('Failed to fetch driver details')
      console.error(err)
    } finally {
      setIsLoading(false)
    }
  }

  const handleApprove = async () => {
    if (!confirm('Are you sure you want to approve this driver?')) return

    setActionLoading(true)
    try {
      await adminAPI.approveDriver(Number(driverId))
      alert('Driver approved successfully!')
      navigate('/')
    } catch (err) {
      const axiosError = err as AxiosError<{ message?: string }>
      alert(axiosError.response?.data?.message || 'Failed to approve driver')
    } finally {
      setActionLoading(false)
    }
  }

  const handleBan = async () => {
    if (!banReason.trim()) {
      alert('Please provide a reason for banning')
      return
    }

    setActionLoading(true)
    try {
      await adminAPI.banDriver(Number(driverId), banReason)
      alert('Driver banned successfully!')
      setShowBanModal(false)
      navigate('/')
    } catch (err) {
      const axiosError = err as AxiosError<{ message?: string }>
      alert(axiosError.response?.data?.message || 'Failed to ban driver')
    } finally {
      setActionLoading(false)
    }
  }

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-xl text-gray-600">Loading driver details...</div>
      </div>
    )
  }

  if (error || !driver) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-xl text-red-600">{error || 'Driver not found'}</div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex justify-between items-center">
          <div className="flex items-center space-x-4">
            <Link to="/" className="text-blue-600 hover:text-blue-800">
              ← Back
            </Link>
            <h1 className="text-2xl font-bold text-gray-900">Driver Details</h1>
          </div>
          <button
            onClick={onLogout}
            className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg transition duration-200"
          >
            Logout
          </button>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Personal Information */}
          <div className="bg-white rounded-lg shadow-sm p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Personal Information</h2>
            <div className="space-y-3">
              <InfoRow label="Full Name" value={driver.personalInfo?.fullName || 'N/A'} />
              <InfoRow label="National ID" value={driver.personalInfo?.nationalId || 'N/A'} />
              <InfoRow label="Phone Number" value={driver.personalInfo?.phoneNumber || 'N/A'} />
              <InfoRow label="Email" value={driver.personalInfo?.email || 'N/A'} />
              <div className="py-2 border-b border-gray-100">
                <div className="flex justify-between items-center mb-2">
                  <span className="text-gray-600 font-medium">Status:</span>
                  <span className={`px-3 py-1 rounded-full text-xs font-semibold ${
                    driver.onboardingState.status === 'Approved' ? 'bg-green-100 text-green-800' :
                    driver.onboardingState.status === 'UnderReview' ? 'bg-yellow-100 text-yellow-800' :
                    driver.onboardingState.status === 'Suspended' ? 'bg-red-100 text-red-800' :
                    driver.onboardingState.status === 'Rejected' ? 'bg-red-100 text-red-800' :
                    'bg-gray-100 text-gray-800'
                  }`}>
                    {driver.onboardingState.status}
                  </span>
                </div>
                
                <div className="mt-2">
                  <div className="flex justify-between items-center text-xs text-gray-500 mb-1">
                    <span>Onboarding Progress</span>
                    <span>{driver.onboardingState.progress}%</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-1.5">
                    <div 
                      className="bg-blue-600 h-1.5 rounded-full" 
                      style={{ width: `${driver.onboardingState.progress}%` }}
                    ></div>
                  </div>
                </div>

                {driver.onboardingState.rejectionReason && (
                  <div className="mt-2 p-2 bg-red-50 border border-red-100 rounded text-sm text-red-700">
                    <span className="font-semibold text-red-900 block">Rejection Reason:</span>
                    {driver.onboardingState.rejectionReason}
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Vehicle Information */}
          <div className="bg-white rounded-lg shadow-sm p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Vehicle Information</h2>
            <div className="space-y-3">
              <InfoRow label="Vehicle Type" value={driver.vehicleInfo?.vehicleType || 'N/A'} />
              <InfoRow label="Brand" value={driver.vehicleInfo?.brand || 'N/A'} />
              <InfoRow label="Model" value={driver.vehicleInfo?.model || 'N/A'} />
              <InfoRow label="Color" value={driver.vehicleInfo?.color || 'N/A'} />
              <InfoRow label="License Plate" value={driver.vehicleInfo?.licensePlate || 'N/A'} />
              <InfoRow label="Year" value={driver.vehicleInfo?.year?.toString() || 'N/A'} />
            </div>
          </div>

          {/* Documents */}
          <div className="bg-white rounded-lg shadow-sm p-6 lg:col-span-2">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Submitted Documents</h2>
            {driver.documents.length === 0 ? (
              <p className="text-gray-500">No documents uploaded yet</p>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {driver.documents.map((doc, index) => (
                  <div key={index} className="border border-gray-200 rounded-lg p-4">
                    <h3 className="font-semibold text-gray-900 mb-2">{doc.type}</h3>
                    {doc.expirationDate && (
                      <p className="text-sm text-gray-500 mb-2">
                        Expires: {new Date(doc.expirationDate).toLocaleDateString()}
                      </p>
                    )}
                    <a
                      href={doc.imageUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-block px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm rounded-lg transition duration-200"
                    >
                      View Document
                    </a>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Actions */}
          {driver.onboardingState.status === 'UnderReview' && (
            <div className="bg-white rounded-lg shadow-sm p-6 lg:col-span-2">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">Actions</h2>
              <div className="flex space-x-4">
                <button
                  onClick={handleApprove}
                  disabled={actionLoading}
                  className="flex-1 bg-green-600 hover:bg-green-700 text-white font-semibold py-3 px-6 rounded-lg transition duration-200 disabled:opacity-50"
                >
                  {actionLoading ? 'Processing...' : 'Approve Driver'}
                </button>
                <button
                  onClick={() => setShowBanModal(true)}
                  disabled={actionLoading}
                  className="flex-1 bg-red-600 hover:bg-red-700 text-white font-semibold py-3 px-6 rounded-lg transition duration-200 disabled:opacity-50"
                >
                  Ban Driver
                </button>
              </div>
            </div>
          )}
        </div>
      </main>

      {/* Ban Modal */}
      {showBanModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full">
            <h3 className="text-xl font-semibold text-gray-900 mb-4">Ban Driver</h3>
            <p className="text-gray-600 mb-4">Please provide a reason for banning this driver:</p>
            <textarea
              value={banReason}
              onChange={(e) => setBanReason(e.target.value)}
              className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-red-500 focus:border-transparent outline-none transition mb-4"
              rows={4}
              placeholder="Enter reason..."
            />
            <div className="flex space-x-4">
              <button
                onClick={() => setShowBanModal(false)}
                className="flex-1 bg-gray-200 hover:bg-gray-300 text-gray-800 font-semibold py-2 px-4 rounded-lg transition duration-200"
              >
                Cancel
              </button>
              <button
                onClick={handleBan}
                disabled={actionLoading}
                className="flex-1 bg-red-600 hover:bg-red-700 text-white font-semibold py-2 px-4 rounded-lg transition duration-200 disabled:opacity-50"
              >
                {actionLoading ? 'Banning...' : 'Confirm Ban'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function InfoRow({ label, value }: InfoRowProps) {
  return (
    <div className="flex justify-between items-center py-2 border-b border-gray-100">
      <span className="text-gray-600 font-medium">{label}:</span>
      <span className="text-gray-900">{value}</span>
    </div>
  )
}

export default DriverDetails
